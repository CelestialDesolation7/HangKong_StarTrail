using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HangKong_StarTrail.Services;
using System.Globalization;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// ChatWindow.xaml 的交互逻辑，实现与Deepseek AI的对话功能
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly IChatService _chatService;
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private const string WelcomeMessage = "欢迎来到星际智者！我是您的AI助手，可以回答任何关于宇宙、星系和天文学的问题。您想了解什么呢？";
        
        public ChatWindow()
        {
            InitializeComponent();
            _chatService = new DeepseekChatService();
            
            // 添加欢迎消息到历史记录
            _chatHistory.Add(new ChatMessage
            {
                Role = "assistant",
                Content = WelcomeMessage
            });
        }

        /// <summary>
        /// 发送按钮点击事件
        /// </summary>
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        /// <summary>
        /// 输入框按键事件，按Enter发送消息
        /// </summary>
        private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendMessage();
            }
        }

        /// <summary>
        /// 快捷提示按钮点击事件
        /// </summary>
        private async void Prompt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string prompt = button.Content.ToString();
                MessageInput.Text = prompt;
                await SendMessage();
            }
        }

        /// <summary>
        /// 发送消息并获取AI回复
        /// </summary>
        private async Task SendMessage()
        {
            string message = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // 添加用户消息到聊天界面
            AddMessageToChatPanel(message, true);
            
            // 添加用户消息到历史记录
            _chatHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = message
            });
            
            // 清空输入框
            MessageInput.Text = string.Empty;
            
            // 显示加载动画
            LoadingIndicator.Visibility = Visibility.Visible;
            
            try
            {
                // 调用AI服务获取回复
                string aiResponse = await _chatService.GetChatResponseAsync(_chatHistory);
                
                // 添加AI回复到聊天界面
                AddMessageToChatPanel(aiResponse, false);
                
                // 添加AI回复到历史记录
                _chatHistory.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = aiResponse
                });
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"与AI通信时发生错误：{ex.Message}", "通信错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 添加错误消息到聊天界面
                AddMessageToChatPanel("抱歉，我遇到了通信问题，请稍后再试。", false);
            }
            finally
            {
                // 隐藏加载动画
                LoadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 添加消息到聊天面板
        /// </summary>
        private void AddMessageToChatPanel(string message, bool isUser)
        {
            Border messageBorder = new Border
            {
                Style = (Style)FindResource(isUser ? "UserMessageStyle" : "AIMessageStyle")
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = messageText;
            ChatHistoryPanel.Children.Add(messageBorder);
            
            // 滚动到底部
            ChatScrollViewer.ScrollToEnd();
        }

        /// <summary>
        /// 标题栏拖动事件
        /// </summary>
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    
    /// <summary>
    /// 字符串转布尔值转换器，用于判断输入框是否有文本
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int length)
            {
                return length > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 