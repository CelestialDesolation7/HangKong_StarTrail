using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HangKong_StarTrail.Models;
using HangKong_StarTrail.Services;

namespace HangKong_StarTrail.Views
{
    public partial class KnowledgeBaseForm : Window
    {
        private readonly IKnowledgeService _knowledgeService;
        private List<KnowledgeItem> _allItems;
        private List<KnowledgeItem> _favorites;

        public KnowledgeBaseForm()
        {
            InitializeComponent();
            _knowledgeService = new KnowledgeService();
            LoadKnowledgeData();
            InitializeTreeView();
        }

        private void LoadKnowledgeData()
        {
            _allItems = _knowledgeService.GetAllKnowledgeItems();
            _favorites = _knowledgeService.GetFavorites();
        }

        private void InitializeTreeView()
        {
            var rootNodes = new List<TreeViewItem>
            {
                CreateCategoryNode("恒星", "star"),
                CreateCategoryNode("行星", "planet"),
                CreateCategoryNode("星系", "galaxy"),
                CreateCategoryNode("黑洞", "blackhole"),
                CreateCategoryNode("其他", "other")
            };

            KnowledgeTree.ItemsSource = rootNodes;
        }

        private TreeViewItem CreateCategoryNode(string title, string category)
        {
            var node = new TreeViewItem { Header = title };
            var items = _allItems.Where(item => item.Category == category).ToList();
            
            foreach (var item in items)
            {
                node.Items.Add(new TreeViewItem { Header = item.Title, Tag = item });
            }
            
            return node;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                InitializeTreeView();
                return;
            }

            var filteredItems = _allItems
                .Where(item => item.Title.ToLower().Contains(searchText) || 
                              item.Content.ToLower().Contains(searchText))
                .ToList();

            var searchNode = new TreeViewItem { Header = "搜索结果" };
            foreach (var item in filteredItems)
            {
                searchNode.Items.Add(new TreeViewItem { Header = item.Title, Tag = item });
            }

            KnowledgeTree.ItemsSource = new[] { searchNode };
        }

        private void KnowledgeTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is KnowledgeItem item)
            {
                ContentTitle.Text = item.Title;
                ContentBrowser.NavigateToString(FormatContent(item));
            }
        }

        private string FormatContent(KnowledgeItem item)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ 
                            font-family: Arial, sans-serif; 
                            line-height: 1.6; 
                            color: #333; 
                            padding: 20px;
                        }}
                        h1 {{ color: #4169E1; }}
                        img {{ max-width: 100%; height: auto; }}
                    </style>
                </head>
                <body>
                    <h1>{item.Title}</h1>
                    <p>{item.Content}</p>
                    {(!string.IsNullOrEmpty(item.ImagePath) ? $"<img src='{item.ImagePath}' alt='{item.Title}'/>" : "")}
                </body>
                </html>";
        }

        private void QuizButton_Click(object sender, RoutedEventArgs e)
        {
            var quizForm = new QuizForm();
            quizForm.ShowDialog();
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var favoritesNode = new TreeViewItem { Header = "收藏夹" };
            foreach (var item in _favorites)
            {
                favoritesNode.Items.Add(new TreeViewItem { Header = item.Title, Tag = item });
            }

            KnowledgeTree.ItemsSource = new[] { favoritesNode };
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 