using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HangKong_StarTrail.Views;
using System.Diagnostics;

namespace HangKong_StarTrail;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);

            // 设置全局未处理异常处理
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Debug.WriteLine($"应用程序域未处理异常: {ex?.Message}");
                MessageBox.Show($"发生未处理的异常: {ex?.Message}\n\n{ex?.StackTrace}", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            this.DispatcherUnhandledException += (sender, e) =>
            {
                Debug.WriteLine($"UI线程未处理异常: {e.Exception.Message}");
                MessageBox.Show($"发生未处理的UI异常: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "UI错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true; // 标记为已处理，防止应用程序崩溃
            };

            Debug.WriteLine("正在创建启动画面...");
            
            // 创建并显示启动画面
            
            AppSplashScreen splashScreen = new AppSplashScreen();
            this.MainWindow = splashScreen;
            splashScreen.Show();
            
            Debug.WriteLine("启动画面已显示");
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"应用程序启动时出现异常: {ex.Message}");
            MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n{ex.StackTrace}", "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
}

