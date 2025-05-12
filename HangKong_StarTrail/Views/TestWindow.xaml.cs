using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HangKong_StarTrail.Views
{
    public partial class TestWindow : Window
    {
        // 题目数据结构
        public class Question
        {
            public string Title { get; set; } = "";
            public string[] Options { get; set; } = new string[4];
            public int CorrectIndex { get; set; } // 0-3
        }

        // 题库（20题）
        private readonly List<Question> _questionBank = new List<Question>
        {
            new Question{ Title="太阳系中最大的行星是？", Options=new[]{"地球","木星","土星","火星"}, CorrectIndex=1 },
            new Question{ Title="被称为红色星球的行星是？", Options=new[]{"金星","火星","木星","土星"}, CorrectIndex=1 },
            new Question{ Title="太阳系中距离太阳最近的行星是？", Options=new[]{"水星","金星","地球","火星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中最大的卫星是？", Options=new[]{"月球","木卫一","土卫六","海卫一"}, CorrectIndex=2 },
            new Question{ Title="太阳系中唯一已知存在生命的行星是？", Options=new[]{"火星","地球","金星","木星"}, CorrectIndex=1 },
            new Question{ Title="太阳系中最热的行星是？", Options=new[]{"金星","水星","地球","火星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中自转最快的行星是？", Options=new[]{"木星","地球","火星","天王星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中最小的行星是？", Options=new[]{"水星","火星","金星","地球"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有光环的行星是？", Options=new[]{"土星","木星","天王星","以上都是"}, CorrectIndex=3 },
            new Question{ Title="太阳系中最靠外的行星是？", Options=new[]{"天王星","冥王星","海王星","土星"}, CorrectIndex=2 },
            new Question{ Title="太阳系中自转方向与众不同的行星是？", Options=new[]{"金星","地球","火星","木星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最大火山的行星是？", Options=new[]{"地球","火星","金星","水星"}, CorrectIndex=1 },
            new Question{ Title="太阳系中有最大峡谷的行星是？", Options=new[]{"火星","地球","木星","金星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最大风暴的行星是？", Options=new[]{"木星","土星","天王星","地球"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最大卫星数量的行星是？", Options=new[]{"木星","土星","天王星","海王星"}, CorrectIndex=1 },
            new Question{ Title="太阳系中有最大密度的行星是？", Options=new[]{"地球","水星","金星","火星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最小密度的行星是？", Options=new[]{"土星","木星","天王星","海王星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最长昼夜的行星是？", Options=new[]{"金星","地球","火星","木星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最大倾角的行星是？", Options=new[]{"天王星","地球","火星","金星"}, CorrectIndex=0 },
            new Question{ Title="太阳系中有最大气压的行星是？", Options=new[]{"金星","地球","木星","火星"}, CorrectIndex=0 },
        };

        // 当前抽取的5题
        private List<Question> _quizQuestions = new List<Question>();
        // 用户选择
        private readonly Dictionary<int, int> _userChoices = new Dictionary<int, int>(); // 题号->选项索引

        // 静态Random，确保每次都真正随机
        private static readonly Random _rand = new Random();

        public TestWindow()
        {
            InitializeComponent();
            Console.WriteLine("TestWindow构造，时间戳：" + DateTime.Now.Ticks);
            GenerateQuiz();
        }

        // 随机抽题并生成Tab
        private void GenerateQuiz()
        {
            _userChoices.Clear();
            _quizQuestions = _questionBank.OrderBy(x => _rand.Next()).Take(5).ToList();
            QuizTab.Items.Clear();
            for (int i = 0; i < _quizQuestions.Count; i++)
            {
                var q = _quizQuestions[i];
                var tab = new TabItem { Header = $"第{i + 1}题" };
                var panel = new StackPanel { Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center };
                var tb = new TextBlock
                {
                    Text = $"{i + 1}. {q.Title}",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0FF")),
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 30),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                panel.Children.Add(tb);
                var btnPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 30), HorizontalAlignment = HorizontalAlignment.Center };
                for (int j = 0; j < 4; j++)
                {
                    var btn = new Button
                    {
                        Content = $"{(char)('A' + j)}. {q.Options[j]}",
                        Tag = new Tuple<int, int>(i, j),
                        Margin = new Thickness(0, 10, 0, 10),
                        Padding = new Thickness(0, 10, 0, 10),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252550")),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0FF")),
                        FontFamily = new FontFamily("Microsoft YaHei"),
                        Height = 60,
                        Width = 400,
                        FontSize = 22,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    btn.Click += Option_Click;
                    btnPanel.Children.Add(btn);
                }
                panel.Children.Add(btnPanel);
                tab.Content = panel;
                QuizTab.Items.Add(tab);
            }
            QuizTab.SelectedIndex = 0;
            QuizTab.Items.Refresh();
        }

        // 选项按钮高亮并记录选择
        private void Option_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            if (!(btn.Tag is Tuple<int, int> tag)) return;
            int qIdx = tag.Item1;
            int optIdx = tag.Item2;
            // 取消同题其他按钮高亮
            var parent = VisualTreeHelper.GetParent(btn);
            while (parent != null && !(parent is StackPanel))
                parent = VisualTreeHelper.GetParent(parent);
            if (parent is StackPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Button b)
                        b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252550"));
                }
            }
            // 当前按钮高亮
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4169E1"));
            // 记录选择
            _userChoices[qIdx] = optIdx;
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
            for (int i = 0; i < _quizQuestions.Count; i++)
            {
                var q = _quizQuestions[i];
                if (_userChoices.TryGetValue(i, out int userAns) && userAns == q.CorrectIndex)
                {
                    score += 20;
                }
                else
                {
                    string user = "未作答";
                    if (_userChoices.TryGetValue(i, out int u))
                        user = $"{(char)('A' + u)}. {q.Options[u]}";
                    string correct = $"{(char)('A' + q.CorrectIndex)}. {q.Options[q.CorrectIndex]}";
                    wrongList.Add($"{i + 1}. {q.Title}\n你的答案：{user}\n正确答案：{correct}\n");
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