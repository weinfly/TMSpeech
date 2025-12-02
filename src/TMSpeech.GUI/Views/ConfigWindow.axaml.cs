using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;
using TMSpeech.GUI.ViewModels;

namespace TMSpeech.GUI.Views
{
    public partial class ConfigWindow : ReactiveWindow<ConfigViewModel>
    {
        public ConfigWindow()
        {
            Trace.WriteLine("ConfigWindow: 开始初始化");
            try
            {
                InitializeComponent();
                Trace.WriteLine("ConfigWindow: InitializeComponent完成");
                
                ViewModel = new ConfigViewModel();
                Trace.WriteLine("ConfigWindow: ViewModel初始化完成");

                // 延迟初始化版本信息，确保控件已经创建
                this.Loaded += (sender, e) =>
                {
                    Trace.WriteLine("ConfigWindow: Loaded事件触发");
                    try
                    {
                        if (runVersion != null)
                        {
                            runVersion.Text = GitVersionInformation.FullSemVer;
                            Trace.WriteLine($"ConfigWindow: 设置版本信息: {GitVersionInformation.FullSemVer}");
                        }
                        else
                        {
                            Trace.WriteLine("ConfigWindow: runVersion控件为null");
                        }

                        if (runInternalVersion != null)
                        {
                            runInternalVersion.Text = GitVersionInformation.ShortSha +
                                                      (GitVersionInformation.UncommittedChanges != "0" ? " (dirty)" : "");
                            Trace.WriteLine($"ConfigWindow: 设置内部版本信息: {GitVersionInformation.ShortSha}");
                        }
                        else
                        {
                            Trace.WriteLine("ConfigWindow: runInternalVersion控件为null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"ConfigWindow: 设置版本信息失败: {ex.Message}");
                        Console.WriteLine($"Failed to set version information: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ConfigWindow: 初始化失败: {ex.Message}");
                Console.WriteLine($"ConfigWindow initialization failed: {ex.Message}");
            }
        }
    }
}