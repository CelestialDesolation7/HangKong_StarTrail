using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;

namespace HangKong_StarTrail.Views
{
    public partial class TestWindow : Window
    {
        // 正确答案字典
        private readonly Dictionary<int, string> _answers = new Dictionary<int, string>
        {
            {0, "Q1B"}, // 第1题B.木星
            {1, "Q2B"}, // 第2题B.火星
            {2, "Q3A"}, // 第3题A.水星
            {3, "Q4C"}, // 第4题C.土卫六
            {4, "Q5B"}  // 第5题B.地球
        };
        // 用户选择
        private readonly Dictionary<int, string> _userChoices = new Dictionary<int, string>();

        public TestWindow()
        {
            InitializeComponent();
        }

        // 选项按钮高亮并记录选择
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
            // 记录选择
            int tabIndex = QuizTab.SelectedIndex;
            _userChoices[tabIndex] = btn.Name;
        }

        // 上一题
        private void PrevTab_Click(object sender, RoutedEventArgs e)
        {
            if (QuizTab.SelectedIndex > 0)
                QuizTab.SelectedIndex--;
            else
                ShowCustomMessage("目前已是第一题");
        }
        // 下一题
        private void NextTab_Click(object sender, RoutedEventArgs e)
        {
            if (QuizTab.SelectedIndex < QuizTab.Items.Count - 1)
                QuizTab.SelectedIndex++;
            else
                ShowCustomMessage("目前已是最后一题");
        }
        // 交卷
        private void SubmitQuiz_Click(object sender, RoutedEventArgs e)
        {
            int score = 0;
            List<string> wrongList = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                if (_userChoices.TryGetValue(i, out string userAns) && userAns == _answers[i])
                {
                    score += 20;
                }
                else
                {
                    string q = "";
                    string correct = "";
                    string user = "未作答";
                    switch (i)
                    {
                        case 0: q = "1. 太阳系中最大的行星是？"; correct = "B. 木星"; break;
                        case 1: q = "2. 被称为红色星球的行星是？"; correct = "B. 火星"; break;
                        case 2: q = "3. 太阳系中距离太阳最近的行星是？"; correct = "A. 水星"; break;
                        case 3: q = "4. 太阳系中最大的卫星是？"; correct = "C. 土卫六"; break;
                        case 4: q = "5. 太阳系中唯一已知存在生命的行星是？"; correct = "B. 地球"; break;
                    }
                    if (_userChoices.TryGetValue(i, out string u))
                    {
                        switch (u)
                        {
                            case "Q1A": user = "A. 地球"; break;
                            case "Q1B": user = "B. 木星"; break;
                            case "Q1C": user = "C. 土星"; break;
                            case "Q1D": user = "D. 火星"; break;
                            case "Q2A": user = "A. 金星"; break;
                            case "Q2B": user = "B. 火星"; break;
                            case "Q2C": user = "C. 木星"; break;
                            case "Q2D": user = "D. 土星"; break;
                            case "Q3A": user = "A. 水星"; break;
                            case "Q3B": user = "B. 金星"; break;
                            case "Q3C": user = "C. 地球"; break;
                            case "Q3D": user = "D. 火星"; break;
                            case "Q4A": user = "A. 月球"; break;
                            case "Q4B": user = "B. 木卫一"; break;
                            case "Q4C": user = "C. 土卫六"; break;
                            case "Q4D": user = "D. 海卫一"; break;
                            case "Q5A": user = "A. 火星"; break;
                            case "Q5B": user = "B. 地球"; break;
                            case "Q5C": user = "C. 金星"; break;
                            case "Q5D": user = "D. 木星"; break;
                        }
                    }
                    wrongList.Add($"{q}\n你的答案：{user}\n正确答案：{correct}\n");
                }
            }
            ShowResultWindow(score, wrongList);
        }

        // 自定义弹窗（主色背景）
        private void ShowCustomMessage(string msg)
        {
            var win = new Window
            {
                Title = "提示",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F1E")),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };
            var grid = new Grid();
            var tb = new TextBlock
            {
                Text = msg,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0FF")),
                FontSize = 22,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            grid.Children.Add(tb);
            win.Content = grid;
            win.ShowDialog();
        }

        // 交卷结果弹窗
        private void ShowResultWindow(int score, List<string> wrongList)
        {
            var win = new Window
            {
                Title = "测试结果",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F0F1E")),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var scoreText = new TextBlock
            {
                Text = $"你的得分：{score}分",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0FF")),
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            Grid.SetRow(scoreText, 0);
            grid.Children.Add(scoreText);
            var wrongBlock = new TextBlock
            {
                Text = wrongList.Count == 0 ? "全部答对，太棒了！" : "错题：\n\n" + string.Join("\n", wrongList),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0FF")),
                FontSize = 20,
                Margin = new Thickness(30, 0, 30, 20),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(wrongBlock, 1);
            grid.Children.Add(wrongBlock);
            win.Content = grid;
            win.ShowDialog();
        }
    }
} 