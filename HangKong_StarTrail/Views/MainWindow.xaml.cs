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

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
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
            var gravitySimulationForm = new GravitySimulationForm();
            gravitySimulationForm.Closed += (s, args) => this.Show(); // 确保关闭时显示主窗口
            gravitySimulationForm.Show();
            this.Hide();
        }

        private void StartLearning_Click(object sender, RoutedEventArgs e)
        {
            var knowledgeBaseForm = new KnowledgeBaseForm();
            knowledgeBaseForm.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenStarTrail_Click(object sender, RoutedEventArgs e)
        {
            // 打开恒星轨迹界面
        }

        private void OpenGalaxy_Click(object sender, RoutedEventArgs e)
        {
            // 打开星系结构界面
        }

        /// <summary>
        /// 学习宇宙知识按钮点击事件
        /// </summary>
        private void OpenKnowledgeBase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KnowledgeBaseForm knowledgeBaseForm = new KnowledgeBaseForm();
                knowledgeBaseForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开知识库界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 星际智者按钮点击事件
        /// </summary>
        private void OpenAIChat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChatWindow chatWindow = new ChatWindow();
                chatWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开星际智者界面时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}