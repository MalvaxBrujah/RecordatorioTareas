using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using RecordatorioTareas.Data;
using RecordatorioTareas.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RecordatorioTareas
{
    public partial class MainWindow : Window
    {
        private TaskRepository _taskRepository;
        private CategoryRepository _categoryRepository;
        private DispatcherTimer _reminderTimer;
        private TaskbarIcon _trayIcon;
        private bool _salirTotalmente = false;
        private string _textoBusqueda = string.Empty;
        private AppSettings _settings;
        private string _settingsFilePath = "";
        private Point _dragStartPoint;



        private class SnoozedReminder
        {
            public int TaskId { get; set; }
            public DateTime When { get; set; }
            public string Message { get; set; } = "";
        }

        private readonly List<SnoozedReminder> _snoozedReminders = new();

        private class TareaSemanaView
        {
            public string DiaTexto { get; set; } = "";
            public string HoraTexto { get; set; } = "";
            public string Title { get; set; } = "";
        }


        public MainWindow()
        {
            InitializeComponent();
            MainTabs.SelectionChanged += MainTabs_SelectionChanged;
            MainTabs.SelectedIndex = 0;
            ActualizarVistas();

            // Icono de bandeja
            _trayIcon = (TaskbarIcon)FindResource("AppTrayIcon");

            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;

            // Carpeta y archivos de datos
            string baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordatorioTareas");

            Directory.CreateDirectory(baseFolder);

            // Config
            _settingsFilePath = Path.Combine(baseFolder, "config.json");
            _settings = SettingsRepository.Load(_settingsFilePath);

            // Datos
            string tasksFile = Path.Combine(baseFolder, "tasks.json");
            string categoriesFile = Path.Combine(baseFolder, "categories.json");

            _taskRepository = new TaskRepository(tasksFile);
            _categoryRepository = new CategoryRepository(categoriesFile);


            RefrescarCategorias();
            RefrescarLista();
            MarcarDiasConTareas();
            ActualizarSemana();


            IniciarTimerRecordatorios();
            RegistrarInicioConWindows();
        }

        // =======================
        //   INICIO CON WINDOWS
        // =======================

        private void RegistrarInicioConWindows()
        {
            try
            {
                string appName = "RecordatorioTareas";
                string exePath = System.Reflection.Assembly
                    .GetExecutingAssembly().Location;

                using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key == null) return;

                if (_settings != null && _settings.StartWithWindows)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch
            {
                // si algo falla, no pasa nada grave
            }
        }


        private void ListaTareas_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (ListaTareas.SelectedItem is TaskItem tarea &&
                ListaTareas.ContextMenu != null)
            {
                // El segundo item es el de marcar completada/incompleta
                if (ListaTareas.ContextMenu.Items[1] is MenuItem mi)
                {
                    mi.Header = tarea.IsCompleted
                        ? "Marcar como incompleta"
                        : "Marcar como completada";
                }
            }
        }


        private void MostrarResumenDiario()
        {
            var todas = _taskRepository.GetAll();
            var hoy = DateTime.Today;

            int pendientesHoy = todas.Count(t => !t.IsCompleted && t.DueDate.Date == hoy);
            int vencidas = todas.Count(t => !t.IsCompleted && t.DueDate.Date < hoy);
            int pendientesTotales = todas.Count(t => !t.IsCompleted);

            string mensaje = $"Pendientes hoy: {pendientesHoy}. " +
                             $"Vencidas: {vencidas}. " +
                             $"Pendientes totales: {pendientesTotales}.";

            _trayIcon?.ShowBalloonTip(
                "Resumen diario de tareas",
                mensaje,
                BalloonIcon.Info);
        }

        private void AbrirConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            var copia = new AppSettings
            {
                DefaultReminderTime = _settings.DefaultReminderTime,
                StartWithWindows = _settings.StartWithWindows,
                ShowDailySummary = _settings.ShowDailySummary
            };

            var ventana = new SettingsWindow(copia)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                _settings = ventana.ResultSettings;
                SettingsRepository.Save(_settingsFilePath, _settings);
                RegistrarInicioConWindows();
            }
        }

        private void ActualizarSemana()
        {
            if (ListaSemana == null)
                return;

            var referencia = CalendarioTareas?.SelectedDate ?? DateTime.Today;

            // Inicio de semana (lunes)
            int delta = DayOfWeek.Monday - referencia.DayOfWeek;
            if (delta > 0) delta -= 7;
            var inicioSemana = referencia.Date.AddDays(delta);
            var finSemana = inicioSemana.AddDays(7);

            var tareas = _taskRepository.GetAll()
                .Where(t => t.DueDate.Date >= inicioSemana && t.DueDate.Date < finSemana)
                .OrderBy(t => t.DueDate)
                .ThenBy(t => t.ReminderTime)
                .ToList();

            var listaSemana = tareas.Select(t => new TareaSemanaView
            {
                DiaTexto = t.DueDate.ToString("dddd dd/MM"),
                HoraTexto = t.ReminderTime.ToString(@"hh\:mm"),
                Title = t.Title
            }).ToList();

            ListaSemana.ItemsSource = listaSemana;
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarVistas();
        }

        private void ActualizarVistas()
        {
            ViewLista.Visibility = MainTabs.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
            ViewCalendario.Visibility = MainTabs.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            ViewSemana.Visibility = MainTabs.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
            ViewConfig.Visibility = MainTabs.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
        }


        private void MarcarDiasConTareas()
        {
            if (CalendarioTareas == null)
                return;

            var tareas = _taskRepository.GetAll();
            var fechasConTareas = tareas
                .Select(t => t.DueDate.Date)
                .Distinct()
                .ToHashSet();

            // Esperamos que el calendario esté cargado visualmente
            CalendarioTareas.Dispatcher.InvokeAsync(() =>
            {
                foreach (var dayButton in CalendarioTareas.FindChildren<CalendarDayButton>())
                {
                    if (dayButton.DataContext is DateTime fecha)
                    {
                        if (fechasConTareas.Contains(fecha.Date))
                        {
                            dayButton.Background = new SolidColorBrush(Color.FromRgb(255, 230, 150)); // amarillito
                            dayButton.FontWeight = FontWeights.Bold;
                        }
                        else
                        {
                            dayButton.ClearValue(Control.BackgroundProperty);
                            dayButton.ClearValue(Control.FontWeightProperty);
                        }
                    }
                }
            });
        }


        private void AplicarEstiloEnDias(HashSet<DateTime> fechasConTareas)
        {
            var month = CalendarioTareas.DisplayDate;

            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);

            foreach (var item in CalendarioTareas.FindChildren<CalendarDayButton>())
            {
                if (item.DataContext is DateTime fecha)
                {
                    if (fechasConTareas.Contains(fecha.Date))
                    {
                        item.Background = new SolidColorBrush(Color.FromRgb(255, 210, 140)); // Color suave
                        item.FontWeight = FontWeights.Bold;
                    }
                    else
                    {
                        item.ClearValue(Control.BackgroundProperty);
                        item.ClearValue(Control.FontWeightProperty);
                    }
                }
            }
        }

        private void CalendarioTareas_Drop(object sender, DragEventArgs e)
        {
            if (CalendarioTareas.SelectedDate is not DateTime fechaDestino)
            {
                MessageBox.Show("Selecciona un día en el calendario antes de soltar la tarea.");
                return;
            }

            if (e.Data.GetData(typeof(TaskItem)) is not TaskItem tarea)
                return;

            tarea.DueDate = fechaDestino.Date;

            // Al mover de fecha, tiene sentido resetear los recordatorios para ese día
            tarea.Reminded2DaysBefore = false;
            tarea.Reminded1DayBefore = false;
            tarea.RemindedSameDay = false;

            _taskRepository.Update(tarea);

            RefrescarLista();
            MarcarDiasConTareas();
            CalendarioTareas_SelectedDatesChanged(null!, null!);
        }


        private void ListaTareasDelDia_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void ListaTareasDelDia_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var pos = e.GetPosition(null);
            var diff = _dragStartPoint - pos;

            if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (ListaTareasDelDia.SelectedItem is not TaskItem tarea)
                return;

            DragDrop.DoDragDrop(ListaTareasDelDia, tarea, DragDropEffects.Move);
        }


        private void CalendarioTareas_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CalendarioTareas.SelectedDate is not DateTime fecha)
                return;

            var tareas = _taskRepository
                .GetAll()
                .Where(t => t.DueDate.Date == fecha.Date)
                .OrderBy(t => t.ReminderTime)
                .ToList();

            ListaTareasDelDia.ItemsSource = tareas;

            ActualizarSemana(); // que la vista semana tome este día como referencia
        }

        private void CalendarioTareas_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            MarcarDiasConTareas();
        }

        private void CalendarioTareas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CalendarioTareas.SelectedDate is not DateTime fecha)
                return;

            // Creamos una nueva tarea para ese día
            var nueva = new TaskItem
            {
                DueDate = fecha.Date,
                ReminderTime = _settings.GetDefaultReminderTimeSpan(),
                IsCompleted = false
            };

            var ventana = new TaskWindow(nueva, _categoryRepository.GetAll())
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();
            if (resultado == true)
            {
                _taskRepository.Add(nueva);
                RefrescarLista();
                MarcarDiasConTareas();
                CalendarioTareas.SelectedDate = fecha.Date;
                CalendarioTareas_SelectedDatesChanged(null!, null!);
            }
        }



        // =======================
        //   REFRESCAR LISTAS
        // =======================

        private void RefrescarLista()
        {
            var tareas = _taskRepository.GetAll().AsEnumerable();

            // Filtro por categoría
            if (ListaCategorias.SelectedItem is Category cat)
            {
                tareas = tareas.Where(t => t.CategoryId == cat.Id);
            }

            // Filtro por texto (título o descripción)
            if (!string.IsNullOrWhiteSpace(_textoBusqueda))
            {
                string texto = _textoBusqueda.ToLowerInvariant();
                tareas = tareas.Where(t =>
                    (!string.IsNullOrEmpty(t.Title) &&
                     t.Title.ToLowerInvariant().Contains(texto)) ||
                    (!string.IsNullOrEmpty(t.Description) &&
                     t.Description.ToLowerInvariant().Contains(texto)));
            }

            var listaFinal = tareas.ToList();

            ListaTareas.ItemsSource = null;
            ListaTareas.ItemsSource = listaFinal;

            ActualizarEstado(listaFinal);
        }

        private void RefrescarCategorias()
        {
            ListaCategorias.ItemsSource = null;
            ListaCategorias.ItemsSource = _categoryRepository.GetAll().ToList();
        }

        // =======================
        //   TAREAS
        // =======================

        private void AgregarTarea_Click(object sender, RoutedEventArgs e)
        {
            var nuevaTarea = new TaskItem
            {
                DueDate = DateTime.Now.AddDays(1),
                ReminderTime = _settings.GetDefaultReminderTimeSpan(),
                IsCompleted = false
            };

            var ventana = new TaskWindow(nuevaTarea, _categoryRepository.GetAll())
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                _taskRepository.Add(nuevaTarea);
                RefrescarLista();
                MarcarDiasConTareas();
                ActualizarSemana();

            }
        }

        private void EditarTarea_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTareas.SelectedItem is not TaskItem tareaSeleccionada)
            {
                MessageBox.Show("Selecciona una tarea de la lista para editar.");
                return;
            }

            var copia = new TaskItem
            {
                Id = tareaSeleccionada.Id,
                Title = tareaSeleccionada.Title,
                Description = tareaSeleccionada.Description,
                DueDate = tareaSeleccionada.DueDate,
                ReminderTime = tareaSeleccionada.ReminderTime,
                CategoryId = tareaSeleccionada.CategoryId,
                IsCompleted = tareaSeleccionada.IsCompleted,
                Reminded2DaysBefore = tareaSeleccionada.Reminded2DaysBefore,
                Reminded1DayBefore = tareaSeleccionada.Reminded1DayBefore,
                RemindedSameDay = tareaSeleccionada.RemindedSameDay
            };

            var ventana = new TaskWindow(copia, _categoryRepository.GetAll())
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                _taskRepository.Update(copia);
                RefrescarLista();
                MarcarDiasConTareas();
                ActualizarSemana();

            }
        }

        private void EliminarTarea_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTareas.SelectedItem is not TaskItem tareaSeleccionada)
            {
                MessageBox.Show("Selecciona una tarea de la lista para eliminar.");
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Seguro que quieres eliminar la tarea:\n\"{tareaSeleccionada.Title}\"?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar == MessageBoxResult.Yes)
            {
                _taskRepository.Delete(tareaSeleccionada.Id);
                RefrescarLista();
                MarcarDiasConTareas();
                ActualizarSemana();

            }
        }

        // =======================
        //   CATEGORÍAS
        // =======================

        private void AgregarCategoria_Click(object sender, RoutedEventArgs e)
        {
            var nombre = TxtNuevaCategoria.Text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Escribe un nombre para la categoría.");
                return;
            }

            var categoria = new Category
            {
                Name = nombre
            };

            _categoryRepository.Add(categoria);
            TxtNuevaCategoria.Text = string.Empty;

            RefrescarCategorias();
        }

        private void ListaCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefrescarLista();
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _textoBusqueda = TxtBuscar.Text.Trim();
            RefrescarLista();
        }

        private void RenombrarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (ListaCategorias.SelectedItem is not Category cat)
            {
                MessageBox.Show("Selecciona una categoría para renombrar.");
                return;
            }

            var nuevoNombre = TxtNuevaCategoria.Text.Trim();
            if (string.IsNullOrEmpty(nuevoNombre))
            {
                MessageBox.Show("Escribe el nuevo nombre en la caja de texto.");
                return;
            }

            var copia = new Category
            {
                Id = cat.Id,
                Name = nuevoNombre
            };

            _categoryRepository.Update(copia);
            TxtNuevaCategoria.Text = string.Empty;

            RefrescarCategorias();
        }

        private void EliminarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (ListaCategorias.SelectedItem is not Category cat)
            {
                MessageBox.Show("Selecciona una categoría para eliminar.");
                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Seguro que quieres eliminar la categoría \"{cat.Name}\"?\n" +
                $"Las tareas asociadas quedarán sin categoría.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes)
                return;

            foreach (var tarea in _taskRepository.GetAll()
                         .Where(t => t.CategoryId == cat.Id).ToList())
            {
                tarea.CategoryId = null;
                _taskRepository.Update(tarea);
            }

            _categoryRepository.Delete(cat.Id);

            ListaCategorias.SelectedItem = null;
            RefrescarCategorias();
            RefrescarLista();
        }

        // =======================
        //   RECORDATORIOS
        // =======================

        private void IniciarTimerRecordatorios()
        {
            // Timer normal: cada minuto
            _reminderTimer = new DispatcherTimer();
            _reminderTimer.Interval = TimeSpan.FromMinutes(1);
            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();

            // Timer de arranque: 5 minutos después
            var startupTimer = new DispatcherTimer();
            startupTimer.Interval = TimeSpan.FromMinutes(5);
            startupTimer.Tick += (s, e) =>
            {
                startupTimer.Stop();

                // Revisamos recordatorios con tolerancia grande (por si el PC estuvo apagado)
                RevisarRecordatoriosInterno(TimeSpan.FromHours(24));

                // Resumen diario si está activado en la config
                if (_settings == null || _settings.ShowDailySummary)
                {
                    MostrarResumenDiario();
                }
            };
            startupTimer.Start();
        }



        private void ReminderTimer_Tick(object? sender, EventArgs e)
        {
            RevisarRecordatorios(); // como ya lo tienes
            RevisarSnoozes();       // nuevo: revisa los pospuestos
        }


        private void RevisarRecordatorios()
        {
            // modo normal: 1 minuto de tolerancia
            RevisarRecordatoriosInterno(TimeSpan.FromMinutes(1));
        }

        private void RevisarRecordatoriosInterno(TimeSpan tolerancia)
        {
            var ahora = DateTime.Now;

            foreach (var tarea in _taskRepository.GetAll().Where(t => !t.IsCompleted))
            {
                var fechaBase = tarea.DueDate.Date;

                var momento2Dias = fechaBase.AddDays(-2).Add(tarea.ReminderTime);
                var momento1Dia = fechaBase.AddDays(-1).Add(tarea.ReminderTime);
                var momento0Dias = fechaBase.Add(tarea.ReminderTime);

                bool cambio = false;

                if (!tarea.Reminded2DaysBefore &&
                    EstaDentroDe(ahora, momento2Dias, tolerancia))
                {
                    MostrarRecordatorio(tarea, "Faltan 2 días para esta tarea");
                    tarea.Reminded2DaysBefore = true;
                    cambio = true;
                }

                if (!tarea.Reminded1DayBefore &&
                    EstaDentroDe(ahora, momento1Dia, tolerancia))
                {
                    MostrarRecordatorio(tarea, "Falta 1 día para esta tarea");
                    tarea.Reminded1DayBefore = true;
                    cambio = true;
                }

                if (!tarea.RemindedSameDay &&
                    EstaDentroDe(ahora, momento0Dias, tolerancia))
                {
                    MostrarRecordatorio(tarea, "¡Hoy vence esta tarea!");
                    tarea.RemindedSameDay = true;
                    cambio = true;
                }

                if (cambio)
                {
                    _taskRepository.Update(tarea);
                }
            }
        }


        private bool DebeDispararRecordatorio(
            DateTime ahora,
            DateTime objetivo,
            TimeSpan tolerancia,
            bool incluirPasados)
        {
            // Caso normal: estamos cerca de la hora exacta
            if (EstaDentroDe(ahora, objetivo, tolerancia))
                return true;

            // Caso "PC estaba apagado": ya pasó la hora, pero aún no se notificó
            if (incluirPasados && objetivo < ahora)
                return true;

            return false;
        }


        private void ListaTareas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListaTareas.SelectedItem is TaskItem)
            {
                // Reutilizamos la lógica de Editar
                EditarTarea_Click(sender, e);
            }
        }

        private void MarcarCompletada_Click(object sender, RoutedEventArgs e)
        {
            if (ListaTareas.SelectedItem is not TaskItem tarea)
            {
                MessageBox.Show("Selecciona una tarea para cambiar su estado.");
                return;
            }

            
            tarea.IsCompleted = !tarea.IsCompleted;
            _taskRepository.Update(tarea);
            RefrescarLista();
        }

        private bool EstaDentroDe(DateTime ahora, DateTime objetivo, TimeSpan tolerancia)
        {
            return Math.Abs((ahora - objetivo).TotalMinutes) <= tolerancia.TotalMinutes;
        }


        private void MostrarRecordatorio(TaskItem tarea, string mensaje)
        {
            // Preguntamos si quiere posponer
            var result = MessageBox.Show(
                $"{mensaje}:\n\n{tarea.Title}\n\n¿Posponer 10 minutos?",
                "Recordatorio de tarea",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                // Guardamos un recordatorio extra en memoria
                _snoozedReminders.Add(new SnoozedReminder
                {
                    TaskId = tarea.Id,
                    When = DateTime.Now.AddMinutes(10),
                    Message = mensaje
                });
            }
            // Si elige No, simplemente no hacemos nada más:
            // los flags de recordatorio ya se marcaron en RevisarRecordatorios
        }

        private void RevisarSnoozes()
        {
            var ahora = DateTime.Now;
            var disparar = _snoozedReminders
                .Where(s => s.When <= ahora)
                .ToList();

            foreach (var s in disparar)
            {
                var tarea = _taskRepository.GetAll().FirstOrDefault(t => t.Id == s.TaskId);
                if (tarea != null)
                {
                    // Volvemos a mostrar el recordatorio, con opción de volver a posponer
                    MostrarRecordatorio(tarea, s.Message);
                }

                _snoozedReminders.Remove(s);
            }
        }


        private void ActualizarEstado(IList<TaskItem> tareasVisibles)
        {
            int total = tareasVisibles.Count;
            int pendientes = tareasVisibles.Count(t => !t.IsCompleted);
            int vencidas = tareasVisibles.Count(t => !t.IsCompleted && t.DueDate.Date < DateTime.Today);

            TxtEstadoTotal.Text = $"Total: {total}";
            TxtEstadoPendientes.Text = $"Pendientes: {pendientes}";
            TxtEstadoVencidas.Text = $"Vencidas: {vencidas}";
        }

        // =======================
        //   BANDEJA DE SISTEMA
        // =======================

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();

                _trayIcon.ShowBalloonTip(
                    "Recordatorio de Tareas",
                    "La aplicación sigue ejecutándose en la bandeja.",
                    BalloonIcon.Info);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_salirTotalmente)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                _trayIcon.Dispose();
            }
        }

        private void Tray_Mostrar_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void Tray_Salir_Click(object sender, RoutedEventArgs e)
        {
            _salirTotalmente = true;
            Close();
        }
    }
}
