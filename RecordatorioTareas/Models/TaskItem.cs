namespace RecordatorioTareas.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;       // Título de la tarea
        public string? Description { get; set; }                // Descripción opcional

        public DateTime DueDate { get; set; }                   // Fecha de vencimiento (con hora 00:00 por ahora)
        public TimeSpan ReminderTime { get; set; }              // Hora del recordatorio (ej: 09:00)

        public int? CategoryId { get; set; }                    // Puede no tener categoría
        public bool IsCompleted { get; set; }                   // Está completada o no

        // Flags para no repetir recordatorios
        public bool Reminded2DaysBefore { get; set; }
        public bool Reminded1DayBefore { get; set; }
        public bool RemindedSameDay { get; set; }

        public bool IsOverdue
        {
            get
            {
                return !IsCompleted && DueDate.Date < DateTime.Today;
            }
        }

        public bool IsDueToday
        {
            get
            {
                return !IsCompleted && DueDate.Date == DateTime.Today;
            }
        }

    }
}
