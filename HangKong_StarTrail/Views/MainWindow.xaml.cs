using System;
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
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private GravitySimulationForm _gravityForm;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserProgress();
        }

        /// <summary>
        /// 加载用户学习进度
        /// </summary>
        private void LoadUserProgress()
        {
            try
            {
                // 这里应该是从数据存储中读取用户的实际进度
                // 此处为模拟数据
                double progressPercentage = 35;
                // 在实际应用中，这里会更新UI上的进度条
            }
            catch (Exception ex)
            {
                // 记录错误但不影响主程序运行
                Console.WriteLine($"加载用户进度出错: {ex.Message}");
            }
        }

        #region 窗口控制

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 确保鼠标左键按下时才能拖动窗口
                if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                {
                    // 确保窗口处于正常状态
                    if (this.WindowState == WindowState.Normal || this.WindowState == WindowState.Maximized)
                    {
                        this.DragMove();
                    }
                    
                    // 标记事件已处理，防止冒泡
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响程序运行
                Console.WriteLine($"窗口拖动时出错: {ex.Message}");
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
                
                // 标记事件已处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"最小化窗口时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Application.Current.Shutdown();
                
                // 标记事件已处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭应用程序时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 功能按钮事件

        private void StartExploration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 记录活动历史（实际应用中应存储到数据库）
                SaveActivityHistory("太阳系探索");
                
                var gravitySimulationForm = new GravitySimulationForm();
                gravitySimulationForm.Closed += (s, args) => this.Show(); // 确保关闭时显示主窗口
                gravitySimulationForm.Show();

                var chartsView = new ChartsView(gravitySimulationForm);
                var window = new Window
                {
                    Title = "仿真图表",
                    Content = chartsView,
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                window.Show();


                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开探索界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 学习宇宙知识按钮点击事件
        /// 打开知识库窗口，显示天文知识内容
        /// </summary>
        private void StartLearning_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 记录活动历史
                SaveActivityHistory("宇宙知识学习");
                
                var knowledgeBaseForm = new KnowledgeBaseForm();
                knowledgeBaseForm.Closed += (s, args) => this.Activate(); // 确保关闭时激活主窗口
                knowledgeBaseForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开知识库界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenStarTrail_Click(object sender, RoutedEventArgs e)
        {
            // 打开恒星轨迹界面
            SaveActivityHistory("恒星轨迹探索");
        }

        private void OpenGalaxy_Click(object sender, RoutedEventArgs e)
        {
            // 打开星系结构界面
            SaveActivityHistory("星系结构探索");
        }

        /// <summary>
        /// 星际智者按钮点击事件
        /// </summary>
        private void OpenAIChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 记录活动历史
                SaveActivityHistory("星际智者对话");
                
                ChatWindow chatWindow = new ChatWindow();
                chatWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开星际智者界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 保存用户活动历史
        /// </summary>
        private void SaveActivityHistory(string activityName)
        {
            try
            {
                // 在实际应用中，这里会将活动保存到数据库
                // 此处仅为示例，实际实现应该更新UI和持久化存储
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"记录活动: {activityName} - {timestamp}");
                
                // 实际应用中重新加载活动列表
                // LoadActivityHistory();
            }
            catch (Exception ex)
            {
                // 记录错误但不影响主程序运行
                Console.WriteLine($"保存活动历史出错: {ex.Message}");
            }
        }

        private void ViewCharts_Click(object sender, RoutedEventArgs e)
        {
            var chartsView = new ChartsView(_gravityForm);
            var window = new Window
            {
                Title = "仿真图表",
                Content = chartsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.Show();
        }

        #endregion
    }
}