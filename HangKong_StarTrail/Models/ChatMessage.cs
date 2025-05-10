using System;

namespace HangKong_StarTrail.Models
{
    /// <summary>
    /// 聊天消息模型类，用于存储AI对话中的消息
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// 消息角色：user(用户)或assistant(AI助手)
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
} 