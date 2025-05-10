using System;
using System.Collections.Generic;

namespace HangKong_StarTrail.Models
{
    /// <summary>
    /// 知识库条目模型
    /// </summary>
    public class KnowledgeItem
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 摘要描述
        /// </summary>
        public string Summary { get; set; } = string.Empty;
        
        /// <summary>
        /// 正文内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// 图片路径
        /// </summary>
        public string? ImagePath { get; set; }
        
        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// 创建日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新日期
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
} 