using System.Windows;
using System.Windows.Input;
using HangKong_StarTrail.Models;
using HangKong_StarTrail.Services;

namespace HangKong_StarTrail.Views
{
    public partial class KnowledgeBaseForm : Window
    {
        private readonly IKnowledgeService _knowledgeService;

        public KnowledgeBaseForm()
        {
            InitializeComponent();
            _knowledgeService = new KnowledgeService(); // 这里需要实现具体的服务类
            LoadAllKnowledge();
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
            this.Close();
        }

        #endregion

        #region 知识库功能

        private void LoadAllKnowledge()
        {
            var items = _knowledgeService.GetAllKnowledgeItems();
            KnowledgeList.ItemsSource = items;
        }

        private void ShowAllKnowledge_Click(object sender, RoutedEventArgs e)
        {
            LoadAllKnowledge();
        }

        private void ShowStarKnowledge_Click(object sender, RoutedEventArgs e)
        {
            var items = _knowledgeService.GetItemsByCategory("恒星");
            KnowledgeList.ItemsSource = items;
        }

        private void ShowGalaxyKnowledge_Click(object sender, RoutedEventArgs e)
        {
            var items = _knowledgeService.GetItemsByCategory("星系");
            KnowledgeList.ItemsSource = items;
        }

        private void ShowPhenomena_Click(object sender, RoutedEventArgs e)
        {
            var items = _knowledgeService.GetItemsByCategory("宇宙现象");
            KnowledgeList.ItemsSource = items;
        }

        private void ShowFavorites_Click(object sender, RoutedEventArgs e)
        {
            var items = _knowledgeService.GetFavorites();
            KnowledgeList.ItemsSource = items;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            var keyword = SearchBox.Text;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var items = _knowledgeService.SearchItems(keyword);
                KnowledgeList.ItemsSource = items;
            }
        }

        #endregion
    }
} 