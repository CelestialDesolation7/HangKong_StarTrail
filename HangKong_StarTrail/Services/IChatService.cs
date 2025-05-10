using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HangKong_StarTrail.Models;

namespace HangKong_StarTrail.Services
{
    /// <summary>
    /// 聊天服务接口，定义与AI聊天相关的方法
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// 获取聊天回复
        /// </summary>
        /// <param name="chatHistory">聊天历史记录</param>
        /// <param name="cancellationToken">取消令牌（可选）</param>
        /// <returns>AI回复内容</returns>
        Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken = default);
    }
} 