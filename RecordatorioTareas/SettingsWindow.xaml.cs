using System;
using System.Globalization;
using System.Windows;

namespace RecordatorioTareas
{
    public partial class SettingsWindow : Window
    {
        public AppSettings ResultSettings { get; private set; }

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();

            // Creamos una copia para editar
            ResultSettings = new AppSettings
            {
                DefaultReminderTime = settings.DefaultReminderTime,
                StartWithWindows = settings.StartWithWindows,
                ShowDailySummary = settings.ShowDailySummary
            };

            TxtHoraDefecto.Text = ResultSettings.DefaultReminderTime;
            ChkInicioWindows.IsChecked = ResultSettings.StartWithWindows;
            ChkResumenDiario.IsChecked = ResultSettings.ShowDailySummary;
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            // Validar hora
            if (!TimeSpan.TryParseExact(
                    TxtHoraDefecto.Text.Trim(),
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out var _))
            {
                MessageBox.Show("La hora debe tener formato HH:MM, por ejemplo 09:00.");
                return;
            }

            ResultSettings.DefaultReminderTime = TxtHoraDefecto.Text.Trim();
            ResultSettings.StartWithWindows = ChkInicioWindows.IsChecked == true;
            ResultSettings.ShowDailySummary = ChkResumenDiario.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
