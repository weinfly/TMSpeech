using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using TMSpeech.Core.Plugins;

namespace TMSpeech.GUI.Controls;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    public static readonly StyledProperty<IEnumerable<TextInfo>> ItemsSourceProperty =
        AvaloniaProperty.Register<HistoryView, IEnumerable<TextInfo>>(
            "ItemsSource");

    public IEnumerable<TextInfo> ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private (int, int) _selectionStart = (-1, 0);
    private (int, int) _selectionEnd = (-1, 0);
    private bool _isPointerPressed;

    private (int, int) GetIndexFromPointerEvent(PointerEventArgs e)
    {
        var hitTestPoint = e.GetPosition(list);
        var hitTestControl = list.InputHitTest(hitTestPoint);
        
        // 转换为Visual类型
        Visual? visualControl = hitTestControl as Visual;
        if (visualControl == null) return (-1, 0);
        
        // 查找父级Grid
        var grid = FindParentGrid(visualControl);
        if (grid == null) return (-1, 0);
        
        // 查找点击的SelectableTextBlock
        SelectableTextBlock? textblock = null;
        foreach (var child in grid.Children)
        {
            if (child is SelectableTextBlock tb)
            {
                // 检查点击位置是否在SelectableTextBlock内
                var tbPos = e.GetPosition(tb);
                var tbBounds = tb.Bounds;
                if (tbPos.X >= 0 && tbPos.X <= tbBounds.Width && tbPos.Y >= 0 && tbPos.Y <= tbBounds.Height)
                {
                    textblock = tb;
                    break;
                }
            }
        }
        
        if (textblock == null) return (-1, 0);
        
        var parent = grid.GetVisualParent();
        if (parent is not Control parentControl) return (-1, 0);
        var index = list.IndexFromContainer(parentControl);
        var pos = e.GetPosition(textblock);
        var element = textblock.TextLayout.HitTestPoint(pos);
        var charIndex = element.TextPosition;
        return (index, charIndex);
    }
    
    private Grid? FindParentGrid(Visual? visual)
    {
        if (visual == null) return null;
        if (visual is Grid grid) return grid;
        return FindParentGrid(visual.GetVisualParent());
    }

    private ((int, int), (int, int)) GetLessAndGreater()
    {
        (int, int) less, greater;

        #region get less and greater

        if (_selectionStart.Item1 < _selectionEnd.Item1)
        {
            less = _selectionStart;
            greater = _selectionEnd;
        }
        else if (_selectionStart.Item1 > _selectionEnd.Item1)
        {
            less = _selectionEnd;
            greater = _selectionStart;
        }
        else
        {
            if (_selectionStart.Item2 < _selectionEnd.Item2)
            {
                less = _selectionStart;
                greater = _selectionEnd;
            }
            else
            {
                less = _selectionEnd;
                greater = _selectionStart;
            }
        }

        #endregion

        return (less, greater);
    }

    private void RenderSelection()
    {
        if (_selectionStart.Item1 == -1 || _selectionEnd.Item1 == -1)
        {
            for (int i = 0; i < list.ItemCount; i++)
            {
                if (list.ContainerFromIndex(i) is not ContentPresenter cp) continue;
                if (cp.Child is not Grid grid) continue;
                
                // 遍历Grid中的所有SelectableTextBlock，清除选择
                foreach (var child in grid.Children.OfType<SelectableTextBlock>())
                {
                    child.SelectionStart = 0;
                    child.SelectionEnd = 0;
                }
            }

            return;
        }

        var (less, greater) = GetLessAndGreater();

        for (var i = 0; i < list.ItemCount; i++)
        {
            if (list.ContainerFromIndex(i) is not ContentPresenter cp) continue;
            if (cp.Child is not Grid grid) continue;
            
            // 遍历Grid中的所有SelectableTextBlock
            foreach (var textblock in grid.Children.OfType<SelectableTextBlock>())
            {
                if (i < less.Item1 || i > greater.Item1)
                {
                    // 不在选择范围内，清除选择
                    textblock.SelectionStart = 0;
                    textblock.SelectionEnd = 0;
                    continue;
                }

                // 在选择范围内，设置选择范围
                var start = i == less.Item1 ? less.Item2 : 0;
                var end = i == greater.Item1 ? greater.Item2 : textblock.Text.Length;
                textblock.SelectionStart = start;
                textblock.SelectionEnd = end;
            }
        }
    }


    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        Console.WriteLine($"{_selectionStart.Item1},{_selectionStart.Item2}");
        _selectionStart = GetIndexFromPointerEvent(e);
        _selectionEnd = (-1, 0);
        _isPointerPressed = true;
        RenderSelection();
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPointerPressed = false;
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPointerPressed) return;
        var p = GetIndexFromPointerEvent(e);
        if (p.Item1 == -1) return;
        _selectionEnd = p;
        RenderSelection();
    }

    public async void Copy()
    {
        string copyText = "";

        var (less, greater) = GetLessAndGreater();

        for (int i = less.Item1; i <= greater.Item1; i++)
        {
            if (list.ContainerFromIndex(i) is not ContentPresenter cp) continue;
            if (cp.Child is not Grid grid) continue;
            
            // 获取Grid中的SelectableTextBlock元素
            var textblocks = grid.Children.OfType<SelectableTextBlock>().ToList();
            if (textblocks.Count < 2) continue;
            
            // 按照时间、英文、分隔符、翻译的格式复制完整内容
            var timeBlock = grid.Children.OfType<TextBlock>().FirstOrDefault(tb => Grid.GetColumn(tb) == 0);
            var englishBlock = textblocks[0]; // 英文文本
            var translateBlock = textblocks[1]; // 翻译文本
            
            string timeStr = timeBlock?.Text ?? "";
            string englishText = englishBlock.Text;
            string translateText = translateBlock.Text;
            
            // 构建完整的复制文本，包括时间、英文、分隔符和翻译
            copyText += $"{timeStr} {englishText} | {translateText}\n";
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        await clipboard.SetTextAsync(copyText);
    }

    public async void SelectAll()
    {
        _selectionStart = (0, 0);
        _selectionEnd = (list.ItemCount - 1, ItemsSource.Last().Text.Length);
        RenderSelection();
    }

    private void Copy_OnClick(object? sender, RoutedEventArgs e)
    {
        Copy();
    }

    private void SelectAll_OnClick(object? sender, RoutedEventArgs e)
    {
        SelectAll();
    }
}