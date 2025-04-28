using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using HangKong_StarTrail.Models;

namespace HangKong_StarTrail.Services
{
    public class KnowledgeService : IKnowledgeService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly List<KnowledgeItem> _knowledgeItems;

        public KnowledgeService()
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "knowledge.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
            _knowledgeItems = new List<KnowledgeItem>
            {
                new KnowledgeItem
                {
                    Id = "1",
                    Title = "恒星的生命周期",
                    Summary = "恒星从诞生到死亡的完整过程",
                    Content = "恒星的生命周期包括：星云阶段、原恒星阶段、主序星阶段、红巨星阶段、白矮星阶段等。",
                    Category = "恒星",
                    IsFavorite = false
                },
                new KnowledgeItem
                {
                    Id = "2",
                    Title = "银河系的结构",
                    Summary = "银河系的基本组成和结构特征",
                    Content = "银河系是一个棒旋星系，由核心、旋臂、银晕等部分组成。",
                    Category = "星系",
                    IsFavorite = false
                },
                new KnowledgeItem
                {
                    Id = "3",
                    Title = "黑洞的形成",
                    Summary = "黑洞的形成机制和基本特征",
                    Content = "黑洞是由大质量恒星坍缩形成的，具有极强的引力。",
                    Category = "宇宙现象",
                    IsFavorite = false
                }
            };
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                            CREATE TABLE KnowledgeItems (
                                Id TEXT PRIMARY KEY,
                                Title TEXT NOT NULL,
                                Category TEXT NOT NULL,
                                Content TEXT NOT NULL,
                                ImagePath TEXT,
                                CreatedDate TEXT NOT NULL,
                                LastModifiedDate TEXT NOT NULL,
                                IsFavorite INTEGER DEFAULT 0,
                                Tags TEXT,
                                VideoUrl TEXT,
                                RelatedItems TEXT
                            )";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<KnowledgeItem> GetAllKnowledgeItems()
        {
            return _knowledgeItems;
        }

        public List<KnowledgeItem> GetFavorites()
        {
            return _knowledgeItems.FindAll(item => item.IsFavorite);
        }

        public KnowledgeItem GetItemById(string id)
        {
            return _knowledgeItems.Find(item => item.Id == id);
        }

        public List<KnowledgeItem> SearchItems(string keyword)
        {
            return _knowledgeItems.FindAll(item => 
                item.Title.Contains(keyword) || 
                item.Summary.Contains(keyword) || 
                item.Content.Contains(keyword));
        }

        public List<KnowledgeItem> GetItemsByCategory(string category)
        {
            return _knowledgeItems.FindAll(item => item.Category == category);
        }

        public void AddToFavorites(string itemId)
        {
            var item = GetItemById(itemId);
            if (item != null)
            {
                item.IsFavorite = true;
            }
        }

        public void RemoveFromFavorites(string itemId)
        {
            var item = GetItemById(itemId);
            if (item != null)
            {
                item.IsFavorite = false;
            }
        }

        public void AddItem(KnowledgeItem item)
        {
            _knowledgeItems.Add(item);
        }

        public void UpdateItem(KnowledgeItem item)
        {
            var index = _knowledgeItems.FindIndex(i => i.Id == item.Id);
            if (index != -1)
            {
                _knowledgeItems[index] = item;
            }
        }

        public void DeleteItem(string itemId)
        {
            var item = GetItemById(itemId);
            if (item != null)
            {
                _knowledgeItems.Remove(item);
            }
        }

        public List<KnowledgeItem> GetAllKnowledgeItemsFromDatabase()
        {
            var items = new List<KnowledgeItem>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM KnowledgeItems", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(ReadKnowledgeItem(reader));
                    }
                }
            }
            return items;
        }

        public List<KnowledgeItem> SearchItemsFromDatabase(string keyword)
        {
            var items = new List<KnowledgeItem>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(
                    "SELECT * FROM KnowledgeItems WHERE Title LIKE @Keyword OR Content LIKE @Keyword", 
                    connection))
                {
                    command.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(ReadKnowledgeItem(reader));
                        }
                    }
                }
            }
            return items;
        }

        public List<KnowledgeItem> GetItemsByCategoryFromDatabase(string category)
        {
            var items = new List<KnowledgeItem>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT * FROM KnowledgeItems WHERE Category = @Category", connection))
                {
                    command.Parameters.AddWithValue("@Category", category);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(ReadKnowledgeItem(reader));
                        }
                    }
                }
            }
            return items;
        }

        private KnowledgeItem ReadKnowledgeItem(SQLiteDataReader reader)
        {
            return new KnowledgeItem
            {
                Id = reader["Id"].ToString(),
                Title = reader["Title"].ToString(),
                Category = reader["Category"].ToString(),
                Content = reader["Content"].ToString(),
                ImagePath = reader["ImagePath"].ToString(),
                CreatedDate = DateTime.Parse(reader["CreatedDate"].ToString()),
                LastModifiedDate = DateTime.Parse(reader["LastModifiedDate"].ToString()),
                IsFavorite = Convert.ToBoolean(reader["IsFavorite"]),
                Tags = reader["Tags"]?.ToString()?.Split(','),
                VideoUrl = reader["VideoUrl"].ToString(),
                RelatedItems = reader["RelatedItems"]?.ToString()?.Split(',')
            };
        }

        private void SetCommandParameters(SQLiteCommand command, KnowledgeItem item)
        {
            command.Parameters.AddWithValue("@Id", item.Id);
            command.Parameters.AddWithValue("@Title", item.Title);
            command.Parameters.AddWithValue("@Category", item.Category);
            command.Parameters.AddWithValue("@Content", item.Content);
            command.Parameters.AddWithValue("@ImagePath", item.ImagePath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedDate", item.CreatedDate.ToString("O"));
            command.Parameters.AddWithValue("@LastModifiedDate", item.LastModifiedDate.ToString("O"));
            command.Parameters.AddWithValue("@IsFavorite", item.IsFavorite);
            command.Parameters.AddWithValue("@Tags", item.Tags != null ? string.Join(",", item.Tags) : (object)DBNull.Value);
            command.Parameters.AddWithValue("@VideoUrl", item.VideoUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RelatedItems", item.RelatedItems != null ? string.Join(",", item.RelatedItems) : (object)DBNull.Value);
        }
    }
} 