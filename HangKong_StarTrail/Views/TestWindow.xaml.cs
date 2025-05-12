using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace HangKong_StarTrail.Views
{
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        // 选项按钮高亮
        private void Option_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            // 取消同题其他按钮高亮
            var parent = VisualTreeHelper.GetParent(btn);
            while (parent != null && !(parent is UniformGrid))
                parent = VisualTreeHelper.GetParent(parent);
            if (parent is UniformGrid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Button b)
                        b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252550"));
                }
            }
            // 当前按钮高亮
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4169E1"));
        }

        // 上一题
        private void PrevTab_Click(object sender, RoutedEventArgs e)
        {
            if (QuizTab.SelectedIndex > 0)
                QuizTab.SelectedIndex--;
        }
        // 下一题
        private void NextTab_Click(object sender, RoutedEventArgs e)
        {
            if (QuizTab.SelectedIndex < QuizTab.Items.Count - 1)
                QuizTab.SelectedIndex++;
        }
        // 交卷
        private void SubmitQuiz_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("已交卷！(静态页面示例)", "提示");
        }
    }
} 