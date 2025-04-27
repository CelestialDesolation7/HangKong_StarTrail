using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace HangKong_StarTrail;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    #region 窗口控制

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    #endregion

    #region 功能按钮事件

    private void StartExploration_Click(object sender, RoutedEventArgs e)
    {
        // 将来连接到主界面
        MessageBox.Show("启动探索之旅功能尚未实现，敬请期待！", "星穹轨道", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadScene_Click(object sender, RoutedEventArgs e)
    {
        // 将来连接到场景加载界面
        MessageBox.Show("载入已有场景功能尚未实现，敬请期待！", "星穹轨道", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void StartLearning_Click(object sender, RoutedEventArgs e)
    {
        // 将来连接到学习模块
        MessageBox.Show("学习宇宙知识功能尚未实现，敬请期待！", "星穹轨道", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        // 将来连接到设置界面
        MessageBox.Show("系统设置功能尚未实现，敬请期待！", "星穹轨道", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    #endregion
}