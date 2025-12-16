using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RecordatorioTareas.Models;
using System.Text.Json;
using RecordatorioTareas.Models;

namespace RecordatorioTareas.Data
{
    public class TaskRepository
    {
        private readonly string _filePath;
        private List<TaskItem> _tasks = new();

        public TaskRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        public IReadOnlyList<TaskItem> GetAll()
        {
            return _tasks;
        }

        public void Add(TaskItem task)
        {
            // Asignar un Id simple incremental
            int newId = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1;
            task.Id = newId;
            _tasks.Add(task);
            Save();
        }

        public void Update(TaskItem task)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing == null) return;

            // Copiar propiedades
            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.DueDate = task.DueDate;
            existing.ReminderTime = task.ReminderTime;
            existing.CategoryId = task.CategoryId;
            existing.IsCompleted = task.IsCompleted;
            existing.Reminded2DaysBefore = task.Reminded2DaysBefore;
            existing.Reminded1DayBefore = task.Reminded1DayBefore;
            existing.RemindedSameDay = task.RemindedSameDay;

            Save();
        }

        public void Delete(int id)
        {
            var existing = _tasks.FirstOrDefault(t => t.Id == id);
            if (existing != null)
            {
                _tasks.Remove(existing);
                Save();
            }
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                _tasks = new List<TaskItem>();
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<TaskItem>>(json);

                _tasks = loaded ?? new List<TaskItem>();
            }
            catch
            {
                // Si algo sale mal leyendo, empezamos con lista vacía
                _tasks = new List<TaskItem>();
            }
        }

        private void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_tasks, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
