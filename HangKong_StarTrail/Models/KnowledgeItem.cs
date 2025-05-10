using System;

namespace HangKong_StarTrail.Models
{
    public class KnowledgeItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        public bool IsFavorite { get; set; }
        public string[]? Tags { get; set; }
        public string? VideoUrl { get; set; }
        public string[]? RelatedItems { get; set; }

        public KnowledgeItem()
        {
            // 默认构造函数已通过属性初始化器设置了默认值
        }
    }
} 