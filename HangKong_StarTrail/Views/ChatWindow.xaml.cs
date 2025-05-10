using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Data;
using System.Threading.Tasks;
using HangKong_StarTrail.Services;
using HangKong_StarTrail.Models;
using System.Globalization;
using System.Text;
using System.Speech.Synthesis;
using System.Threading;
using System.IO;

namespace HangKong_StarTrail.Views
{
    /// <summary>
    /// ChatWindow.xaml 的交互逻辑，实现与Deepseek AI的对话功能，支持流式输出和语音朗读
    /// </summary>
    public partial class ChatWindow : Window
    {
        private readonly IChatService _chatService;
        private List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private const string WelcomeMessage = "欢迎来到星际智者！我是您的AI助手，可以回答任何关于宇宙、星系和天文学的问题。您想了解什么呢？";
        
        // 语音合成器
        private SpeechSynthesizer? _speechSynthesizer;
        private bool _isSpeechInputActive = false;
        private string _lastAIResponse = string.Empty;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 用于控制流式输出的常量
        private const int NORMAL_CHAR_DELAY = 20; // ms
        private const int PUNCTUATION_DELAY = 100; // ms
        
        public ChatWindow()
        {
            InitializeComponent();
            
            _chatService = new DeepseekChatService();
            
            // 初始化语音功能
            InitializeSpeechComponents();
            
            // 添加欢迎消息到历史记录
            _chatHistory.Add(new ChatMessage
            {
                Role = "assistant",
                Content = WelcomeMessage
            });
        }
        
        /// <summary>
        /// 初始化语音组件
        /// </summary>
        private void InitializeSpeechComponents()
        {
            try
            {
                // 初始化语音合成器
                _speechSynthesizer = new SpeechSynthesizer();
                
                // 设置中文女声（如果可用）
                try
                {
                    var voices = _speechSynthesizer.GetInstalledVoices();
                    var chineseVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh-"));
                    if (chineseVoice != null)
                    {
                        _speechSynthesizer.SelectVoice(chineseVoice.VoiceInfo.Name);
                    }
                }
                catch
                {
                    // 找不到中文语音，使用默认语音
                }
                
                // 设置语音属性
                _speechSynthesizer.Rate = 0; // 正常语速
                _speechSynthesizer.Volume = 100; // 最大音量
            }
            catch (Exception ex)
            {
                // 如果语音初始化失败，禁用语音按钮
                if (ReadAloudButton != null)
                    ReadAloudButton.IsEnabled = false;
                MessageBox.Show($"语音组件初始化失败: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            // 禁用语音输入按钮，我们将使用简化实现
            if (SpeechInputButton != null)
                SpeechInputButton.IsEnabled = false;
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
                string prompt = button.Content.ToString() ?? string.Empty;
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

            // 停止任何正在播放的语音
            _speechSynthesizer?.SpeakAsyncCancelAll();
            
            // 添加用户消息到聊天界面
            AddUserMessageToChatPanel(message);
            
            // 添加用户消息到历史记录
            _chatHistory.Add(new ChatMessage
            {
                Role = "user",
                Content = message
            });
            
            // 清空输入框
            MessageInput.Text = string.Empty;
            
            // 禁用发送按钮和输入框
            SendButton.IsEnabled = false;
            MessageInput.IsEnabled = false;
            
            // 显示思考状态指示器
            ThinkingIndicator.Visibility = Visibility.Visible;
            
            try
            {
                // 设置取消令牌
                _cancellationTokenSource = new CancellationTokenSource();
                
                // 调用AI服务获取回复
                string aiResponse = await _chatService.GetChatResponseAsync(_chatHistory, _cancellationTokenSource.Token);
                
                // 保存最后的AI回复用于语音朗读
                _lastAIResponse = aiResponse;
                
                // 以流式效果显示AI回复
                await AnimateAIResponseAsync(aiResponse);
                
                // 添加AI回复到历史记录
                _chatHistory.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = aiResponse
                });
            }
            catch (TaskCanceledException)
            {
                // 请求被取消，不做处理
            }
            catch (Exception ex)
            {
                // 显示错误消息
                MessageBox.Show($"与AI通信时发生错误：{ex.Message}", "通信错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 添加错误消息到聊天界面
                AddAIMessageToChatPanel("抱歉，我遇到了通信问题，请稍后再试。");
            }
            finally
            {
                // 隐藏思考状态指示器
                ThinkingIndicator.Visibility = Visibility.Collapsed;
                
                // 启用发送按钮和输入框
                SendButton.IsEnabled = true;
                MessageInput.IsEnabled = true;
                MessageInput.Focus();
            }
        }

        /// <summary>
        /// 添加用户消息到聊天面板
        /// </summary>
        private void AddUserMessageToChatPanel(string message)
        {
            Border messageBorder = new Border
            {
                Style = (Style)FindResource("UserMessageStyle")
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = messageText;
            
            // 添加消息进入动画
            messageBorder.Opacity = 0;
            messageBorder.Margin = new Thickness(120, 5, 10, 5);
            
            ChatHistoryPanel.Children.Add(messageBorder);
            
            // 执行动画
            messageBorder.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromMilliseconds(300)));
            messageBorder.BeginAnimation(Border.MarginProperty, new System.Windows.Media.Animation.ThicknessAnimation(
                new Thickness(80, 5, 10, 5), TimeSpan.FromMilliseconds(300)));
            
            // 滚动到底部
            ChatScrollViewer.ScrollToEnd();
        }
        
        /// <summary>
        /// 添加AI消息到聊天面板（即时显示，不带动画）
        /// </summary>
        private void AddAIMessageToChatPanel(string message)
        {
            Border messageBorder = new Border
            {
                Style = (Style)FindResource("AIMessageStyle")
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
        /// 创建空的AI消息框并返回TextBlock引用
        /// </summary>
        private TextBlock CreateEmptyAIMessagePanel()
        {
            Border messageBorder = new Border
            {
                Style = (Style)FindResource("AIMessageStyle")
            };

            TextBlock messageText = new TextBlock
            {
                Text = "",
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = messageText;
            
            // 添加消息进入动画
            messageBorder.Opacity = 0;
            messageBorder.Margin = new Thickness(10, 5, 130, 5);
            
            ChatHistoryPanel.Children.Add(messageBorder);
            
            // 执行动画
            messageBorder.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(1, TimeSpan.FromMilliseconds(300)));
            messageBorder.BeginAnimation(Border.MarginProperty, new System.Windows.Media.Animation.ThicknessAnimation(
                new Thickness(10, 5, 80, 5), TimeSpan.FromMilliseconds(300)));
            
            return messageText;
        }
        
        /// <summary>
        /// 流式显示AI回复
        /// </summary>
        private async Task AnimateAIResponseAsync(string response)
        {
            if (string.IsNullOrEmpty(response))
                return;
                
            // 创建一个空的AI消息面板
            TextBlock messageText = CreateEmptyAIMessagePanel();
            
            // 逐字显示回复文本
            StringBuilder displayedText = new StringBuilder();
            foreach (char c in response)
            {
                // 检查是否被取消
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                    break;
                    
                displayedText.Append(c);
                messageText.Text = displayedText.ToString();
                
                // 根据字符类型设置不同的延迟
                int delay = IsPunctuation(c) ? PUNCTUATION_DELAY : NORMAL_CHAR_DELAY;
                
                // 等待一段时间后显示下一个字符
                await Task.Delay(delay);
                
                // 滚动到底部
                ChatScrollViewer.ScrollToEnd();
            }
        }
        
        /// <summary>
        /// 判断字符是否为需要额外延迟的标点符号
        /// </summary>
        private bool IsPunctuation(char c)
        {
            return c == '。' || c == '，' || c == '！' || c == '？' || c == '；' || 
                   c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || 
                   c == '\n';
        }
        
        /// <summary>
        /// 语音输入按钮点击事件
        /// </summary>
        private void SpeechInputButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("语音输入功能尚在开发中，请使用键盘输入", "功能未开放", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 语音朗读按钮点击事件
        /// </summary>
        private void ReadAloudButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_lastAIResponse))
                    return;
                    
                // 停止当前朗读
                _speechSynthesizer?.SpeakAsyncCancelAll();
                
                // 开始朗读最后的AI回复
                _speechSynthesizer?.SpeakAsync(_lastAIResponse);
                
                // 更改按钮样式
                ReadAloudButton.Background = new SolidColorBrush(Color.FromRgb(150, 95, 255));
                
                // 朗读完成后恢复按钮样式
                if (_speechSynthesizer != null)
                {
                    _speechSynthesizer.SpeakCompleted += (s, args) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ReadAloudButton.Background = new SolidColorBrush(Color.FromRgb(37, 32, 66)) { Opacity = 0.8 };
                        });
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"语音朗读功能出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            // 取消任何正在进行的任务
            _cancellationTokenSource?.Cancel();
            
            // 停止语音播放
            _speechSynthesizer?.SpeakAsyncCancelAll();
            
            // 释放语音资源
            _speechSynthesizer?.Dispose();
            
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