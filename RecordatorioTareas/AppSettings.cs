namespace RecordatorioTareas
{
    public class AppSettings
    {
        // Guardamos la hora por defecto como texto "HH:mm"
        public string DefaultReminderTime { get; set; } = "09:00";

        public bool StartWithWindows { get; set; } = true;
        public bool ShowDailySummary { get; set; } = true;

        public TimeSpan GetDefaultReminderTimeSpan()
        {
            if (TimeSpan.TryParse(DefaultReminderTime, out var ts))
                return ts;

            return new TimeSpan(9, 0, 0);
        }
    }
}
