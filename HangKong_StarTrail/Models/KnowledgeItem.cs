using System;

namespace HangKong_StarTrail.Models
{
    public class KnowledgeItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public string ImagePath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsFavorite { get; set; }
        public string[] Tags { get; set; }
        public string VideoUrl { get; set; }
        public string[] RelatedItems { get; set; }
    }
} 