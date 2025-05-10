using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HangKong_StarTrail.Models;
using System.Windows;
using System.IO;
using System.Configuration;

namespace HangKong_StarTrail.Services
{
    /// <summary>
    /// Deepseek聊天服务，实现与Deepseek API的通信
    /// </summary>
    public class DeepseekChatService : IChatService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://api.deepseek.com/chat/completions";
        private string _apiKey = string.Empty;
        private const string DefaultSystemPrompt = "你是一个友好的星际AI助手，专注于天文学和宇宙知识。你将以简洁、准确、易懂的方式回答用户关于宇宙、星系、行星和天文现象的问题。回答应具有科学准确性，同时保持对话的趣味性。";
        
        public DeepseekChatService()
        {
            InitializeApiKey();
        }

        /// <summary>
        /// 初始化API密钥，从配置文件或环境变量中获取
        /// </summary>
        private void InitializeApiKey()
        {
            try
            {
                // 尝试从配置文件获取API密钥
                var settings = ConfigurationManager.AppSettings;
                _apiKey = settings["DeepseekApiKey"];

                // 如果配置文件中没有，则尝试从环境变量获取
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
                }

                // 如果还是没有，则从本地文件获取
                if (string.IsNullOrEmpty(_apiKey))
                {
                    string apiKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_key.txt");
                    if (File.Exists(apiKeyPath))
                    {
                        // 读取API密钥并清除所有空白字符（包括换行符、空格等）
                        _apiKey = File.ReadAllText(apiKeyPath)
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Replace(" ", "")
                            .Trim();
                        
                        // 确保移除了注释行
                        if (_apiKey.Contains("#"))
                        {
                            var lines = _apiKey.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines.Length > 0)
                            {
                                // 取最后一个非空部分，通常是实际的API密钥
                                _apiKey = lines[lines.Length - 1].Trim();
                            }
                        }
                    }
                }

                // 如果以上方式都无法获取API密钥，则提示用户
                if (string.IsNullOrEmpty(_apiKey))
                {
                    MessageBox.Show("未找到Deepseek API密钥，请在api_key.txt文件中配置或设置环境变量DEEPSEEK_API_KEY", 
                        "配置错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    // 创建一个空的api_key.txt文件，以便用户知道在哪里配置
                    string apiKeyDir = AppDomain.CurrentDomain.BaseDirectory;
                    string apiKeyPath = Path.Combine(apiKeyDir, "api_key.txt");
                    File.WriteAllText(apiKeyPath, "# 请在此处填入您的Deepseek API密钥\n# 获取方式：访问 https://platform.deepseek.com");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化API密钥时出错：{ex.Message}", "配置错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取聊天回复
        /// </summary>
        /// <param name="chatHistory">聊天历史记录</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>AI回复内容</returns>
        public async Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("未配置Deepseek API密钥，无法使用AI聊天功能");
            }

            try
            {
                // 构建请求消息
                var messages = new List<Dictionary<string, string>>();
                
                // 添加系统指令
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", DefaultSystemPrompt }
                });
                
                // 添加历史消息，最多保留最近10条
                int startIndex = Math.Max(0, chatHistory.Count - 10);
                for (int i = startIndex; i < chatHistory.Count; i++)
                {
                    var message = chatHistory[i];
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", message.Role },
                        { "content", message.Content }
                    });
                }

                // 构建请求内容
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages,
                    stream = false,
                    temperature = 0.7,
                    max_tokens = 2000
                };

                // 序列化为JSON
                string jsonContent = JsonSerializer.Serialize(requestBody);
                
                // 创建请求
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // 确保API密钥没有换行符或其他非法字符
                string sanitizedApiKey = _apiKey.Trim();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sanitizedApiKey);

                // 发送请求，并添加取消令牌支持
                var response = await _httpClient.PostAsync(ApiUrl, httpContent, cancellationToken);
                string responseContent = await response.Content.ReadAsStringAsync();

                // 检查响应状态
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"API请求失败：{response.StatusCode}, {responseContent}");
                }

                // 解析响应
                var jsonResponse = JsonDocument.Parse(responseContent);
                var choices = jsonResponse.RootElement.GetProperty("choices");
                var responseMessage = choices[0].GetProperty("message");
                var content = responseMessage.GetProperty("content").GetString();

                return content ?? "无法获取回复内容";
            }
            catch (OperationCanceledException)
            {
                // 请求被取消，直接原样抛出异常
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"调用Deepseek API失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取流式聊天回复
        /// </summary>
        /// <param name="chatHistory">聊天历史记录</param>
        /// <param name="onMessageReceived">接收流式消息的回调函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>完成任务</returns>
        public async Task GetStreamingChatResponseAsync(List<ChatMessage> chatHistory, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("未配置Deepseek API密钥，无法使用AI聊天功能");
            }

            try
            {
                // 构建请求消息
                var messages = new List<Dictionary<string, string>>();
                
                // 添加系统指令
                messages.Add(new Dictionary<string, string>
                {
                    { "role", "system" },
                    { "content", DefaultSystemPrompt }
                });
                
                // 添加历史消息，最多保留最近10条
                int startIndex = Math.Max(0, chatHistory.Count - 10);
                for (int i = startIndex; i < chatHistory.Count; i++)
                {
                    var message = chatHistory[i];
                    messages.Add(new Dictionary<string, string>
                    {
                        { "role", message.Role },
                        { "content", message.Content }
                    });
                }

                // 构建请求内容，设置stream为true启用流式输出
                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages,
                    stream = true,
                    temperature = 0.7,
                    max_tokens = 2000
                };

                // 序列化为JSON
                string jsonContent = JsonSerializer.Serialize(requestBody);
                
                // 创建请求
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // 确保API密钥没有换行符或其他非法字符
                string sanitizedApiKey = _apiKey.Trim();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sanitizedApiKey);

                // 发送请求，获取流式响应
                using (var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Content = httpContent
                })
                {
                    // 设置响应头读取模式
                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    
                    // 检查响应状态
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"API请求失败：{response.StatusCode}, {errorContent}");
                    }

                    // 获取响应流
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        // 读取流式响应数据
                        string line;
                        StringBuilder messageBuilder = new StringBuilder();
                        
                        while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                        {
                            // 检查是否为保持连接的空行
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }

                            // 检查是否为数据行
                            if (line.StartsWith("data: "))
                            {
                                string data = line.Substring(6);
                                
                                // 检查流是否结束
                                if (data == "[DONE]")
                                {
                                    break;
                                }

                                try
                                {
                                    // 解析JSON数据
                                    var jsonData = JsonDocument.Parse(data);
                                    if (jsonData.RootElement.TryGetProperty("choices", out var choices) &&
                                        choices.GetArrayLength() > 0)
                                    {
                                        var choice = choices[0];
                                        if (choice.TryGetProperty("delta", out var delta) &&
                                            delta.TryGetProperty("content", out var content))
                                        {
                                            string contentText = content.GetString() ?? string.Empty;
                                            if (!string.IsNullOrEmpty(contentText))
                                            {
                                                // 调用回调函数，传递收到的内容片段
                                                onMessageReceived(contentText);
                                                messageBuilder.Append(contentText);
                                            }
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    Console.WriteLine($"解析流式数据时发生错误: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 请求被取消，直接原样抛出异常
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"调用Deepseek流式API失败：{ex.Message}", ex);
            }
        }
    }
} 