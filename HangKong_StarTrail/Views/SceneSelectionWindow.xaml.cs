using System;
using System.Collections.Generic;
using System.Windows;
using System.Data.SQLite;
using System.IO;

namespace HangKong_StarTrail.Views
{
    public partial class SceneSelectionWindow : Window
    {
        private string _selectedSceneName;
        public string SelectedSceneName => _selectedSceneName;

        public SceneSelectionWindow()
        {
            InitializeComponent();
            LoadScenes();
        }

        private void LoadScenes()
        {
            try
            {
                string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\scene.db");
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("未找到预设场景数据库", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand("SELECT SceneName FROM Scenes ORDER BY SceneName", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SceneComboBox.Items.Add(reader.GetString(0));
                            }
                        }
                    }
                }

                if (SceneComboBox.Items.Count > 0)
                {
                    SceneComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载预设场景时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (SceneComboBox.SelectedItem != null)
            {
                _selectedSceneName = SceneComboBox.SelectedItem.ToString();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("请选择一个预设场景", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}