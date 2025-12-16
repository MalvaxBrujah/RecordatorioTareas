using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RecordatorioTareas.Models;

namespace RecordatorioTareas.Data
{
    public class CategoryRepository
    {
        private readonly string _filePath;
        private List<Category> _categories = new();

        public CategoryRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        public IReadOnlyList<Category> GetAll()
        {
            return _categories;
        }

        public void Add(Category category)
        {
            int newId = _categories.Any() ? _categories.Max(c => c.Id) + 1 : 1;
            category.Id = newId;
            _categories.Add(category);
            Save();
        }

        public void Update(Category category)
        {
            var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
            if (existing == null) return;

            existing.Name = category.Name;
            Save();
        }

        public void Delete(int id)
        {
            var existing = _categories.FirstOrDefault(c => c.Id == id);
            if (existing != null)
            {
                _categories.Remove(existing);
                Save();
            }
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                _categories = new List<Category>();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<Category>>(json);
                _categories = loaded ?? new List<Category>();
            }
            catch
            {
                _categories = new List<Category>();
            }
        }

        private void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_categories, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
