using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HangKong_StarTrail.Models;
using HangKong_StarTrail.Services;

namespace HangKong_StarTrail.Views
{
    public partial class QuizForm : Window
    {
        private readonly IKnowledgeService _knowledgeService;
        private List<QuizQuestion> _questions;
        private int _currentQuestionIndex;
        private Dictionary<int, int> _userAnswers;
        private int _score;

        public QuizForm()
        {
            InitializeComponent();
            _knowledgeService = new KnowledgeService();
            _userAnswers = new Dictionary<int, int>();
            LoadQuestions();
            ShowQuestion(0);
        }

        private void LoadQuestions()
        {
            // 从知识库中随机选择10个问题
            var allItems = _knowledgeService.GetAllKnowledgeItems();
            var random = new Random();
            _questions = allItems
                .OrderBy(x => random.Next())
                .Take(10)
                .Select(item => new QuizQuestion
                {
                    Question = $"关于{item.Title}，以下哪项描述是正确的？",
                    Options = GenerateOptions(item, allItems, random),
                    CorrectOptionIndex = 0
                })
                .ToList();
        }

        private List<string> GenerateOptions(KnowledgeItem correctItem, List<KnowledgeItem> allItems, Random random)
        {
            var options = new List<string> { correctItem.Content };
            
            // 从其他知识项中随机选择3个错误选项
            var wrongOptions = allItems
                .Where(item => item.Id != correctItem.Id)
                .OrderBy(x => random.Next())
                .Take(3)
                .Select(item => item.Content)
                .ToList();

            options.AddRange(wrongOptions);
            
            // 随机打乱选项顺序
            return options.OrderBy(x => random.Next()).ToList();
        }

        private void ShowQuestion(int index)
        {
            if (index < 0 || index >= _questions.Count)
                return;

            _currentQuestionIndex = index;
            var question = _questions[index];

            QuestionText.Text = question.Question;
            OptionsList.ItemsSource = question.Options.Select((text, i) => new { Text = text, Index = i });
            
            // 恢复用户之前的选择
            if (_userAnswers.ContainsKey(index))
            {
                var radioButton = FindRadioButtonByIndex(_userAnswers[index]);
                if (radioButton != null)
                    radioButton.IsChecked = true;
            }
            else
            {
                ClearSelection();
            }

            // 更新进度
            ProgressBar.Value = (index + 1) * 100.0 / _questions.Count;
            ProgressText.Text = $"第 {index + 1} 题 / 共 {_questions.Count} 题";

            // 更新按钮状态
            PreviousButton.IsEnabled = index > 0;
            NextButton.Content = index == _questions.Count - 1 ? "完成" : "下一题";
        }

        private RadioButton FindRadioButtonByIndex(int index)
        {
            var container = OptionsList.ItemContainerGenerator.ContainerFromIndex(index) as ContentPresenter;
            return container?.ContentTemplate.FindName("OptionRadioButton", container) as RadioButton;
        }

        private void ClearSelection()
        {
            foreach (var item in OptionsList.Items)
            {
                var container = OptionsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                var radioButton = container?.ContentTemplate.FindName("OptionRadioButton", container) as RadioButton;
                if (radioButton != null)
                    radioButton.IsChecked = false;
            }
        }

        private void Option_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null)
            {
                var index = OptionsList.Items.IndexOf(radioButton.DataContext);
                _userAnswers[_currentQuestionIndex] = index;
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            ShowQuestion(_currentQuestionIndex - 1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentQuestionIndex == _questions.Count - 1)
            {
                CalculateScore();
                ShowResult();
            }
            else
            {
                ShowQuestion(_currentQuestionIndex + 1);
            }
        }

        private void CalculateScore()
        {
            _score = 0;
            for (int i = 0; i < _questions.Count; i++)
            {
                if (_userAnswers.ContainsKey(i) && 
                    _userAnswers[i] == _questions[i].CorrectOptionIndex)
                {
                    _score++;
                }
            }
        }

        private void ShowResult()
        {
            var result = MessageBox.Show(
                $"测验完成！\n得分：{_score}/{_questions.Count}\n正确率：{_score * 100.0 / _questions.Count:F1}%",
                "测验结果",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.OK)
            {
                this.Close();
            }
        }
    }

    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectOptionIndex { get; set; }
    }
} 