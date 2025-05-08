using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HangKong_StarTrail.Views;

namespace HangKong_StarTrail;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 创建并显示启动画面
        AppSplashScreen splashScreen = new AppSplashScreen();
        this.MainWindow = splashScreen;
        splashScreen.Show();

        // 修改启动URI以避免自动启动MainWindow
        this.StartupUri = null;
    }
}

