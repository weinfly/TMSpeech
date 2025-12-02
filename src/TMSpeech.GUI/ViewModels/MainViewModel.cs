using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TMSpeech.Core;
using TMSpeech.Core.Plugins;
using TMSpeech.Core.Services.Notification;

namespace TMSpeech.GUI.ViewModels;

public class CaptionStyleViewModel : ViewModelBase
{
    [ObservableAsProperty]
    public int ShadowSize { get; }

    [ObservableAsProperty]
    public Color ShadowColor { get; }

    [ObservableAsProperty]
    public int FontSize { get; }

    [ObservableAsProperty]
    public Color FontColor { get; }

    [ObservableAsProperty]
    public TextAlignment TextAlign { get; }

    [ObservableAsProperty]
    public FontFamily FontFamily { get; }

    [ObservableAsProperty]
    public Color MouseHover { get; }

    [ObservableAsProperty]
    public Color BackgroundColor { get; }

    [ObservableAsProperty]
    public string Text { get; }

    private IObservable<T> GetPropObservable<T>(string key)
    {
        return Observable.Return(ConfigManagerFactory.Instance.Get<T>(key))
            .Merge(
                Observable.FromEventPattern<ConfigChangedEventArgs>(
                        p => ConfigManagerFactory.Instance.ConfigChanged += p,
                        p => ConfigManagerFactory.Instance.ConfigChanged -= p)
                    .Where(x => x.EventArgs.Contains(key))
                    .Select(x =>
                        ConfigManagerFactory.Instance.Get<T>(key)
                    ));
    }

    public CaptionStyleViewModel(MainViewModel mainViewModel)
    {
        GetPropObservable<int>(AppearanceConfigTypes.ShadowSize)
            .ToPropertyEx(this, x => x.ShadowSize);
        GetPropObservable<uint>(AppearanceConfigTypes.ShadowColor)
            .Select(Color.FromUInt32)
            .ToPropertyEx(this, x => x.ShadowColor);
        GetPropObservable<uint>(AppearanceConfigTypes.BackgroundColor)
            .Select(Color.FromUInt32)
            .ToPropertyEx(this, x => x.BackgroundColor);
        GetPropObservable<int>(AppearanceConfigTypes.FontSize)
            .Select(x => { return x; })
            .ToPropertyEx(this, x => x.FontSize);
        GetPropObservable<uint>(AppearanceConfigTypes.FontColor)
            .Select(Color.FromUInt32)
            .ToPropertyEx(this, x => x.FontColor);
        GetPropObservable<int>(AppearanceConfigTypes.TextAlign)
            .Select(x => x switch
            {
                AppearanceConfigTypes.TextAlignEnum.Left => TextAlignment.Left,
                AppearanceConfigTypes.TextAlignEnum.Center => TextAlignment.Center,
                AppearanceConfigTypes.TextAlignEnum.Right => TextAlignment.Right,
                AppearanceConfigTypes.TextAlignEnum.Justify => TextAlignment.Right,
                _ => TextAlignment.Left
            })
            .ToPropertyEx(this, x => x.TextAlign);

        GetPropObservable<string>(AppearanceConfigTypes.FontFamily)
            .Select(x => new FontFamily(x))
            .ToPropertyEx(this, x => x.FontFamily);

        GetPropObservable<uint>(AppearanceConfigTypes.MouseHover)
            .Select(Color.FromUInt32)
            .CombineLatest(mainViewModel.WhenAnyValue(x => x.IsLocked),
                (color, locked) => locked ? Colors.Transparent : color)
            .ToPropertyEx(this, x => x.MouseHover);
    }
}

public class MainViewModel : ViewModelBase
{
    [ObservableAsProperty]
    public JobStatus Status { get; }

    [ObservableAsProperty]
    public bool PlayButtonVisible { get; }

    [ObservableAsProperty]
    public bool PauseButtonVisible { get; }

    [ObservableAsProperty]
    public bool StopButtonVisible { get; }

    [ObservableAsProperty]
    public bool HistroyPanelVisible { get; }

    [ObservableAsProperty]
    public long RunningSeconds { get; }

    [ObservableAsProperty]
    public string RunningTimeDisplay { get; }

    public CaptionStyleViewModel CaptionStyle { get; }

    [ObservableAsProperty]
    public string Text { get; }

    [ObservableAsProperty]
    public string TranslatedText { get; }

    [Reactive]
    public bool IsLocked { get; set; }

    public ObservableCollection<TextInfo> HistoryTexts { get; } = new();

    public ReactiveCommand<Unit, Unit> PlayCommand { get; }
    public ReactiveCommand<Unit, Unit> PauseCommand { get; }
    public ReactiveCommand<Unit, Unit> StopCommand { get; }
    public ReactiveCommand<Unit, Unit> LockCommand { get; }

    private readonly JobManager _jobManager;

    public MainViewModel()
    {
        _jobManager = JobManagerFactory.Instance;
        CaptionStyle = new CaptionStyleViewModel(this);

        Observable.FromEventPattern<JobStatus>(
                p => { _jobManager.StatusChanged += p; },
                p => { _jobManager.StatusChanged -= p; }
            )
            .Select(x => x.EventArgs)
            .Merge(Observable.Return(_jobManager.Status))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToPropertyEx(this, x => x.Status);

        this.WhenAnyValue(x => x.Status) // IObservable<JobStatus>
            .Select(x => x == JobStatus.Stopped || x == JobStatus.Paused) // IObservable<bool>
            .ToPropertyEx(this, x => x.PlayButtonVisible);

        this.WhenAnyValue(x => x.Status)
            .Select(x => x == JobStatus.Running)
            .ToPropertyEx(this, x => x.PauseButtonVisible);

        this.WhenAnyValue(x => x.Status)
            .Select(x => x == JobStatus.Running || x == JobStatus.Paused)
            .ToPropertyEx(this, x => x.StopButtonVisible);

        this.LockCommand = ReactiveCommand.Create(() => { 
            IsLocked = true;
            // Inform user if user uses it for the first time.
            var lockedShown = ConfigManagerFactory.Instance.Get<bool>(NotificationConfigTypes.HasShownLockUsage);
            if (!lockedShown)
            {
                ConfigManagerFactory.Instance.Apply(NotificationConfigTypes.HasShownLockUsage, true);
                NotificationManager.Instance.Notify("锁定成功", "右键托盘图标以解锁", NotificationType.Info);
            }
        });

        this.PlayCommand = ReactiveCommand.CreateFromTask(
            async () => { await Task.Run(() => { _jobManager.Start(); }); },
            this.WhenAnyValue(x => x.PlayButtonVisible));
        this.PauseCommand = ReactiveCommand.CreateFromTask(
            async () => { await Task.Run(() => { _jobManager.Pause(); }); },
            this.WhenAnyValue(x => x.PauseButtonVisible));
        this.StopCommand = ReactiveCommand.CreateFromTask(
            async () => { await Task.Run(() => { _jobManager.Stop(); }); },
            this.WhenAnyValue(x => x.StopButtonVisible));

        Observable.FromEventPattern<long>(x => _jobManager.RunningSecondsChanged += x,
                x => _jobManager.RunningSecondsChanged -= x)
            .Select(x => x.EventArgs)
            .ToPropertyEx(this, x => x.RunningSeconds);

        this.WhenAnyValue(x => x.RunningSeconds)
            .Select(x => string.Format("{0:D2}:{1:D2}:{2:D2}", x / 60 / 60, (x / 60) % 60, x % 60))
            .ToPropertyEx(this, x => x.RunningTimeDisplay);

        var textObservable = Observable.FromEventPattern<SpeechEventArgs>(
                p => _jobManager.TextChanged += p,
                p => _jobManager.TextChanged -= p)
            .Select(x => x.EventArgs.Text.Text)
            .Merge(this.PlayCommand.ThrownExceptions.Select(e => $"启动失败！{e.Message}"))
            .Merge(this.PauseCommand.ThrownExceptions.Select(e => $"暂停失败！{e.Message}"))
            .Merge(this.StopCommand.ThrownExceptions.Select(e => $"停止失败！{e.Message}"))
            .Merge(Observable.Return("欢迎使用TMSpeech"));
        
        textObservable.ToPropertyEx(this, x => x.Text);
        
        // 翻译逻辑 - 异步执行，避免阻塞UI
        // 调整Throttle时间为100ms，提高翻译响应速度
        textObservable
            .Throttle(TimeSpan.FromMilliseconds(100)) // 100ms内只处理一次翻译请求，提高响应速度
            .DistinctUntilChanged() // 避免重复翻译相同的文本
            .ObserveOn(RxApp.MainThreadScheduler) // 确保在主线程上处理
            .SelectMany(async text => {
                Trace.WriteLine($"MainViewModel: 收到文本变化，开始翻译: {text}");
                try
                {
                    var pluginManager = PluginManagerFactory.GetInstance();
                    Trace.WriteLine($"MainViewModel: 可用翻译器数量: {pluginManager.Translators.Count}");
                    
                    if (pluginManager.Translators.Count > 0)
                    {
                        // 从配置中获取用户选择的翻译器
                        var selectedTranslatorId = ConfigManagerFactory.Instance.Get<string>("translator");
                        Trace.WriteLine($"MainViewModel: 用户选择的翻译器ID: {selectedTranslatorId}");
                        
                        ITranslator translator;
                        
                        // 如果用户选择了翻译器且可用，则使用该翻译器
                        if (!string.IsNullOrEmpty(selectedTranslatorId) && pluginManager.Translators.TryGetValue(selectedTranslatorId, out translator))
                        {
                            Trace.WriteLine($"MainViewModel: 使用用户选择的翻译器: {selectedTranslatorId}");
                            // 使用Task.Run将翻译操作移到后台线程
                            var result = await Task.Run(() => translator.Translate(text));
                            Trace.WriteLine($"MainViewModel: 翻译完成，结果: {result}");
                            return result;
                        }
                        // 否则使用第一个可用的翻译器
                        else if (pluginManager.Translators.Count > 0)
                        {
                            translator = pluginManager.Translators.First().Value;
                            Trace.WriteLine($"MainViewModel: 使用第一个可用的翻译器: {translator.Name}");
                            var result = await Task.Run(() => translator.Translate(text));
                            Trace.WriteLine($"MainViewModel: 翻译完成，结果: {result}");
                            return result;
                        }
                    }
                    Trace.WriteLine($"MainViewModel: 没有可用的翻译器，返回原始文本");
                    return text;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"MainViewModel: 翻译失败: {ex.Message}");
                    Console.WriteLine($"翻译失败: {ex.Message}");
                    return text;
                }
            })
            .ToPropertyEx(this, x => x.TranslatedText);

        Observable.FromEventPattern<SpeechEventArgs>(
                p => _jobManager.SentenceDone += p,
                p => _jobManager.SentenceDone -= p)
            .Select(x => x.EventArgs.Text)
            .Subscribe(x => { this.HistoryTexts.Add(x); });
    }
}