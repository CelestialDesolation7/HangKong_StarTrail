using System.Collections.Generic;
using HangKong_StarTrail.Models;

namespace HangKong_StarTrail.Services
{
    public interface IKnowledgeService
    {
        List<KnowledgeItem> GetAllKnowledgeItems();
        List<KnowledgeItem> GetFavorites();
        KnowledgeItem? GetItemById(string id);
        List<KnowledgeItem> SearchItems(string keyword);
        List<KnowledgeItem> GetItemsByCategory(string category);
        void AddToFavorites(string itemId);
        void RemoveFromFavorites(string itemId);
        void AddItem(KnowledgeItem item);
        void UpdateItem(KnowledgeItem item);
        void DeleteItem(string itemId);
    }
} 