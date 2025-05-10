using System.Collections.Generic;
using HangKong_StarTrail.Models;

namespace HangKong_StarTrail.Services
{
    /// <summary>
    /// 知识库服务接口
    /// </summary>
    public interface IKnowledgeService
    {
        /// <summary>
        /// 获取所有知识条目
        /// </summary>
        List<KnowledgeItem> GetAllItems();
        
        /// <summary>
        /// 按分类获取知识条目
        /// </summary>
        List<KnowledgeItem> GetItemsByCategory(string category);
        
        /// <summary>
        /// 搜索知识条目
        /// </summary>
        List<KnowledgeItem> SearchItems(string keyword);
        
        /// <summary>
        /// 获取收藏的知识条目
        /// </summary>
        List<KnowledgeItem> GetFavoriteItems();
        
        /// <summary>
        /// 获取所有分类
        /// </summary>
        List<string> GetAllCategories();
        
        /// <summary>
        /// 添加到收藏
        /// </summary>
        void AddToFavorites(string itemId);
        
        /// <summary>
        /// 从收藏移除
        /// </summary>
        void RemoveFromFavorites(string itemId);
        
        /// <summary>
        /// 获取知识条目详情
        /// </summary>
        KnowledgeItem GetItemById(string id);
    }
} 