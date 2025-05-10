using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HangKong_StarTrail.Models;
using HangKong_StarTrail.Services;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// KnowledgeBaseForm.xaml 的交互逻辑
    /// </summary>
    public partial class KnowledgeBaseForm : Window
    {
        private readonly IKnowledgeService _knowledgeService;
        private KnowledgeItem _currentItem;

        public KnowledgeBaseForm()
        {
            InitializeComponent();
            
            // 初始化服务
            _knowledgeService = new KnowledgeService();
            
            // 加载数据
            LoadCategories();
            LoadAllKnowledge();
            
            // 设置窗口标题
            Title = "星穹知识库";
            
            // 注册窗口加载事件
            Loaded += (s, e) => {
                Debug.WriteLine("知识库窗口已加载");
            };
        }
        
        /// <summary>
        /// 加载所有分类
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                var categories = _knowledgeService.GetAllCategories();
                Debug.WriteLine($"加载分类: {categories.Count} 个");
                CategoryList.ItemsSource = categories;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载分类出错: {ex.Message}");
                MessageBox.Show($"加载分类时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 加载所有知识
        /// </summary>
        private void LoadAllKnowledge()
        {
            try
            {
                var items = _knowledgeService.GetAllItems();
                Debug.WriteLine($"加载所有知识: {items.Count} 条");
                KnowledgeListView.ItemsSource = items;
                ContentTitle.Text = "全部知识";
                CategoryList.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载知识出错: {ex.Message}");
                MessageBox.Show($"加载知识内容时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载指定分类的知识
        /// </summary>
        private void LoadKnowledgeByCategory(string category)
        {
            try
            {
                var items = _knowledgeService.GetItemsByCategory(category);
                Debug.WriteLine($"加载分类 {category} 的知识: {items.Count} 条");
                KnowledgeListView.ItemsSource = items;
                ContentTitle.Text = $"{category}知识";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"按分类加载知识出错: {ex.Message}");
                MessageBox.Show($"加载分类知识时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        }
        
        /// <summary>
        /// 加载收藏的知识
        /// </summary>
        private void LoadFavorites()
        {
            try
            {
                var items = _knowledgeService.GetFavoriteItems();
                Debug.WriteLine($"加载收藏知识: {items.Count} 条");
                KnowledgeListView.ItemsSource = items;
                ContentTitle.Text = "我的收藏";
                CategoryList.SelectedItem = null;
        }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载收藏知识出错: {ex.Message}");
                MessageBox.Show($"加载收藏知识时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 搜索知识
        /// </summary>
        private void SearchKnowledge(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    LoadAllKnowledge();
                    return;
                }
                
                var items = _knowledgeService.SearchItems(keyword);
                Debug.WriteLine($"搜索 '{keyword}': 找到 {items.Count} 条结果");
                KnowledgeListView.ItemsSource = items;
                ContentTitle.Text = $"搜索: {keyword}";
                CategoryList.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"搜索知识出错: {ex.Message}");
                MessageBox.Show($"搜索知识时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 显示知识详情
        /// </summary>
        private void ShowKnowledgeDetail(KnowledgeItem item)
        {
            if (item == null) return;
            
            try
            {
                _currentItem = item;
                
                // 设置详情内容
                DetailTitle.Text = item.Title;
                DetailCategory.Text = item.Category;
                DetailContent.Text = item.Content;
                DetailPanel.Tag = item.IsFavorite; // 用于收藏状态显示
                
                // 设置标签
                TagsPanel.ItemsSource = item.Tags;
                
                // 显示详情面板
                DetailPanel.Visibility = Visibility.Visible;
                
                Debug.WriteLine($"显示知识详情: {item.Id} - {item.Title}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"显示知识详情出错: {ex.Message}");
                MessageBox.Show($"显示知识详情时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        /// <summary>
        /// 全部分类按钮点击事件
        /// </summary>
        private void AllCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            LoadAllKnowledge();
        }

        /// <summary>
        /// 分类选择事件
        /// </summary>
        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryList.SelectedItem is string category)
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                LoadKnowledgeByCategory(category);
        }
        }
        
        /// <summary>
        /// 收藏按钮点击事件
        /// </summary>
        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            LoadFavorites();
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
            SearchKnowledge(SearchTextBox.Text);
        }
        
        /// <summary>
        /// 知识条目选择事件
        /// </summary>
        private void KnowledgeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KnowledgeListView.SelectedItem is KnowledgeItem item)
            {
                ShowKnowledgeDetail(item);
        }
        }
        
        /// <summary>
        /// 返回按钮点击事件
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            DetailPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 收藏按钮点击事件
        /// </summary>
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentItem == null) return;
            
            try
            {
                if (_currentItem.IsFavorite)
                {
                    _knowledgeService.RemoveFromFavorites(_currentItem.Id);
                    _currentItem.IsFavorite = false;
                    DetailPanel.Tag = false;
                    Debug.WriteLine($"取消收藏: {_currentItem.Id} - {_currentItem.Title}");
                }
                else
                {
                    _knowledgeService.AddToFavorites(_currentItem.Id);
                    _currentItem.IsFavorite = true;
                    DetailPanel.Tag = true;
                    Debug.WriteLine($"添加收藏: {_currentItem.Id} - {_currentItem.Title}");
            }
        }
            catch (Exception ex)
            {
                Debug.WriteLine($"收藏操作出错: {ex.Message}");
                MessageBox.Show($"处理收藏操作时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 标题栏拖动事件
        /// </summary>
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
                Debug.WriteLine($"窗口拖动时出错: {ex.Message}");
            }
        }
    }
} 