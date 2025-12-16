using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using RecordatorioTareas.Models;

namespace RecordatorioTareas
{
    public partial class TaskWindow : Window
    {
        public TaskItem Task { get; private set; }
        private List<Category> _categories;

        public TaskWindow(TaskItem task, IEnumerable<Category> categories)
        {
            InitializeComponent();

            Task = task;
            _categories = categories.ToList();

            // Cargar categorías en el ComboBox
            CmbCategoria.ItemsSource = _categories;

            // Cargar datos de la tarea en los controles
            TxtTitulo.Text = Task.Title;
            TxtDescripcion.Text = Task.Description ?? string.Empty;

            PickerFecha.SelectedDate =
                Task.DueDate == default ? DateTime.Now.Date : Task.DueDate.Date;

            TxtHora.Text = Task.ReminderTime == default
                ? "09:00"
                : $"{(int)Task.ReminderTime.TotalHours:00}:{Task.ReminderTime.Minutes:00}";

            if (Task.CategoryId.HasValue)
            {
                CmbCategoria.SelectedValue = Task.CategoryId.Value;
            }

            ChkCompletada.IsChecked = Task.IsCompleted;
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTitulo.Text))
            {
                MessageBox.Show("El título no puede estar vacío.");
                return;
            }

            if (PickerFecha.SelectedDate == null)
            {
                MessageBox.Show("Debes seleccionar una fecha de vencimiento.");
                return;
            }

            if (!TimeSpanTryParseHHMM(TxtHora.Text, out var hora))
            {
                MessageBox.Show("La hora debe tener formato HH:MM, por ejemplo 09:00.");
                return;
            }

            if (CmbCategoria.SelectedValue is not int catId)
            {
                MessageBox.Show("Debes seleccionar una categoría para la tarea.");
                return;
            }

            Task.Title = TxtTitulo.Text.Trim();
            Task.Description = string.IsNullOrWhiteSpace(TxtDescripcion.Text)
                ? null
                : TxtDescripcion.Text.Trim();

            Task.DueDate = PickerFecha.SelectedDate.Value.Date;
            Task.ReminderTime = hora;
            Task.IsCompleted = ChkCompletada.IsChecked == true;

            Task.CategoryId = catId;

            DialogResult = true;
            Close();
        }


        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool TimeSpanTryParseHHMM(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(
                text,
                @"hh\:mm",
                CultureInfo.InvariantCulture,
                out time
            );
        }
    }
}
