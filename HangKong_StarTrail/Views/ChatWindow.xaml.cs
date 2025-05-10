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
using System.Speech.Recognition;
using System.Threading;
using System.IO;
using System.Linq;

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
        private string _lastAIResponse = string.Empty;
        private CancellationTokenSource? _cancellationTokenSource;
        
        // 语音识别引擎
        private SpeechRecognitionEngine? _speechRecognizer;
        private bool _isListening = false;

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
                
                // 初始化语音识别引擎
                InitializeSpeechRecognition();
            }
            catch (Exception ex)
            {
                // 如果语音初始化失败，禁用语音按钮
                if (ReadAloudButton != null)
                    ReadAloudButton.IsEnabled = false;
                MessageBox.Show($"语音组件初始化失败: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// 初始化语音识别引擎
        /// </summary>
        private void InitializeSpeechRecognition()
        {
            try
            {
                // 创建语音识别引擎，优先使用中文识别
                RecognizerInfo recognizerInfo = null;
                
                // 尝试查找中文识别器
                foreach (var recognizer in SpeechRecognitionEngine.InstalledRecognizers())
                {
                    if (recognizer.Culture.Name.StartsWith("zh-"))
                    {
                        recognizerInfo = recognizer;
                        break;
                    }
                }
                
                // 如果没有找到中文识别器，使用默认识别器
                if (recognizerInfo == null && SpeechRecognitionEngine.InstalledRecognizers().Count > 0)
                {
                    recognizerInfo = SpeechRecognitionEngine.InstalledRecognizers()[0];
                }
                
                if (recognizerInfo != null)
                {
                    _speechRecognizer = new SpeechRecognitionEngine(recognizerInfo);
                    
                    // 添加常用天文术语到语法中，提高识别准确度
                    var builder = new GrammarBuilder();
                    builder.Culture = recognizerInfo.Culture;
                    
                    // 设置为听写模式（不限制词汇）
                    var dictationGrammar = new DictationGrammar();
                    dictationGrammar.Name = "Dictation Grammar";
                    _speechRecognizer.LoadGrammar(dictationGrammar);
                    
                    // 添加天文相关术语（可以增强识别准确度）
                    var choices = new Choices(new string[] {
                        "星系", "行星", "恒星", "黑洞", "太阳系", "宇宙", "银河系", "地球", 
                        "火星", "木星", "土星", "金星", "水星", "天王星", "海王星", "冥王星",
                        "红矮星", "白矮星", "中子星", "超新星", "引力波", "暗物质", "暗能量",
                        "望远镜", "卫星", "小行星", "彗星", "流星", "星云", "星团"
                    });
                    var astronomyGrammar = new Grammar(new GrammarBuilder(choices));
                    astronomyGrammar.Name = "Astronomy Grammar";
                    _speechRecognizer.LoadGrammar(astronomyGrammar);
                    
                    // 设置事件处理器
                    _speechRecognizer.SpeechRecognized += SpeechRecognizer_SpeechRecognized;
                    _speechRecognizer.SpeechRecognitionRejected += SpeechRecognizer_SpeechRecognitionRejected;
                    _speechRecognizer.SpeechDetected += SpeechRecognizer_SpeechDetected;
                    
                    // 启用语音输入按钮
                    if (SpeechInputButton != null)
                        SpeechInputButton.IsEnabled = true;
                }
                else
                {
                    // 没有找到可用的语音识别器，禁用语音输入
                    if (SpeechInputButton != null)
                        SpeechInputButton.IsEnabled = false;
                    
                    MessageBox.Show("未找到可用的语音识别引擎，语音输入功能不可用", "功能受限", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // 如果语音识别初始化失败，禁用语音输入按钮
                if (SpeechInputButton != null)
                    SpeechInputButton.IsEnabled = false;
                
                MessageBox.Show($"语音识别初始化失败: {ex.Message}", "功能受限", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// 语音被检测到事件处理
        /// </summary>
        private void SpeechRecognizer_SpeechDetected(object sender, SpeechDetectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 更新UI以指示正在接收语音
                SpeechInputButton.Background = new SolidColorBrush(Color.FromRgb(255, 100, 100));
            });
        }
        
        /// <summary>
        /// 语音识别成功事件处理
        /// </summary>
        private void SpeechRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Confidence > 0.3) // 设置置信度阈值
            {
                string recognizedText = e.Result.Text ?? string.Empty;
                
                Dispatcher.Invoke(() =>
                {
                    // 将识别的文本添加到输入框中
                    MessageInput.Text = recognizedText;
                    
                    // 如果置信度高，可以自动发送
                    if (e.Result.Confidence > 0.7 && recognizedText.Length > 3)
                    {
                        SendMessage().ConfigureAwait(false);
                    }
                    
                    // 恢复按钮样式
                    UpdateSpeechButtonStyle(false);
                });
            }
        }
        
        /// <summary>
        /// 语音识别被拒绝事件处理
        /// </summary>
        private void SpeechRecognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // 恢复按钮样式
                UpdateSpeechButtonStyle(false);
                
                // 如果识别失败但有候选项，显示最佳候选项
                if (e.Result != null && e.Result.Words.Count > 0)
                {
                    StringBuilder text = new StringBuilder();
                    foreach (var word in e.Result.Words)
                    {
                        if (word != null && word.Text != null)
                        {
                            text.Append(word.Text + " ");
                        }
                    }
                    
                    if (text.Length > 0)
                    {
                        MessageInput.Text = text.ToString().Trim();
                    }
                }
                else
                {
                    // 识别完全失败，显示提示
                    MessageBox.Show("未能识别您的语音，请尝试重新说话或使用键盘输入", "识别失败", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            });
        }
        
        /// <summary>
        /// 更新语音按钮样式
        /// </summary>
        private void UpdateSpeechButtonStyle(bool isListening)
        {
            _isListening = isListening;
            
            if (isListening)
            {
                // 正在监听状态
                SpeechInputButton.Background = new SolidColorBrush(Color.FromRgb(80, 200, 120));
                SpeechInputButton.ToolTip = "正在听取您的语音，点击停止";
            }
            else
            {
                // 非监听状态
                SpeechInputButton.Background = new SolidColorBrush(Color.FromRgb(37, 32, 66)) { Opacity = 0.8 };
                SpeechInputButton.ToolTip = "语音输入";
                
                // 停止识别引擎
                if (_speechRecognizer != null)
                {
                    try
                    {
                        _speechRecognizer.RecognizeAsyncStop();
                    }
                    catch
                    {
                        // 忽略停止过程中的错误
                    }
                }
            }
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
                
                // 创建一个空的AI消息面板用于流式显示
                TextBlock messageText = CreateEmptyAIMessagePanel();
                StringBuilder responseBuilder = new StringBuilder();
                
                // 调用流式输出API
                await _chatService.GetStreamingChatResponseAsync(
                    _chatHistory, 
                    (partialResponse) => {
                        // 使用Dispatcher确保在UI线程上更新
                        Dispatcher.Invoke(() => {
                            // 追加新收到的文本片段
                            responseBuilder.Append(partialResponse);
                            messageText.Text = responseBuilder.ToString();
                            
                            // 滚动到底部
                            ChatScrollViewer.ScrollToEnd();
                        });
                    },
                    _cancellationTokenSource.Token
                );
                
                // 保存最后的AI回复用于语音朗读
                _lastAIResponse = responseBuilder.ToString();
                
                // 添加AI回复到历史记录
                _chatHistory.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = _lastAIResponse
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
        /// 注意：该方法已被直接调用Deepseek流式API替代，保留仅作为参考
        /// </summary>
        private async Task AnimateAIResponseAsync(string response)
        {
            // 此方法已不再使用，被真正的流式输出API替代
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
            if (_speechRecognizer == null)
            {
                MessageBox.Show("语音识别功能未能初始化，请检查系统是否支持语音识别", "功能不可用", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                if (!_isListening)
                {
                    // 开始监听
                    _speechRecognizer.SetInputToDefaultAudioDevice();
                    _speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                    UpdateSpeechButtonStyle(true);
                    
                    // 停止任何正在播放的语音
                    _speechSynthesizer?.SpeakAsyncCancelAll();
                }
                else
                {
                    // 停止监听
                    _speechRecognizer.RecognizeAsyncStop();
                    UpdateSpeechButtonStyle(false);
                }
            }
            catch (Exception ex)
            {
                UpdateSpeechButtonStyle(false);
                MessageBox.Show($"语音识别出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                Console.WriteLine($"窗口拖动时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 最小化按钮点击事件
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
                
                // 标记事件已处理
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"最小化窗口时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            
            // 停止语音识别
            if (_speechRecognizer != null)
            {
                try
                {
                    _speechRecognizer.RecognizeAsyncStop();
                    _speechRecognizer.Dispose();
                }
                catch
                {
                    // 忽略关闭过程中的错误
                }
            }
            
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