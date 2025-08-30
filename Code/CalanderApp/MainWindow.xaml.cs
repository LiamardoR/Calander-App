using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;

namespace CalendarTaskApp
{
    public partial class MainWindow : Window
    {
        private DateTime _currentDate;
        private int _selectedDay;
        private Dictionary<string, List<TaskItem>> _tasks; // Changed to string key for date persistence
        private string _selectedColor = "#ea4335"; // Default red color
        private readonly string _tasksFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CalendarTaskApp", "tasks.txt");

        public MainWindow()
        {
            InitializeComponent();

            // Set current date and selected day to today
            var today = DateTime.Today;
            _currentDate = new DateTime(today.Year, today.Month, 1);
            _selectedDay = today.Day;

            _tasks = new Dictionary<string, List<TaskItem>>();

            LoadTasks(); // Load saved tasks
            InitializeTimeSelectors();
            GenerateCalendar();
            UpdateTasksDisplay();
        }

        private void InitializeTimeSelectors()
        {
            // Populate hours (1-12)
            for (int i = 1; i <= 12; i++)
            {
                HourComboBox.Items.Add(i.ToString("D2"));
            }
            HourComboBox.SelectedIndex = 11; // Default to 12

            // Populate minutes (00, 15, 30, 45)
            MinuteComboBox.Items.Add("00");
            MinuteComboBox.Items.Add("15");
            MinuteComboBox.Items.Add("30");
            MinuteComboBox.Items.Add("45");
            MinuteComboBox.SelectedIndex = 0; // Default to 00

            // Populate AM/PM
            AmPmComboBox.Items.Add("AM");
            AmPmComboBox.Items.Add("PM");
            AmPmComboBox.SelectedIndex = 1; // Default to PM (12 PM)
        }

        private void LoadTasks()
        {
            try
            {
                if (File.Exists(_tasksFilePath))
                {
                    var lines = File.ReadAllLines(_tasksFilePath);
                    _tasks = new Dictionary<string, List<TaskItem>>();

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split('|');
                        if (parts.Length == 4)
                        {
                            var date = parts[0];
                            var name = parts[1];
                            var color = parts[2];
                            var time = parts[3];

                            if (!_tasks.ContainsKey(date))
                            {
                                _tasks[date] = new List<TaskItem>();
                            }

                            _tasks[date].Add(new TaskItem
                            {
                                Name = name,
                                Color = color,
                                Time = time
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                _tasks = new Dictionary<string, List<TaskItem>>();
            }
        }

        private void SaveTasks()
        {
            try
            {
                var directory = Path.GetDirectoryName(_tasksFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = new List<string>();
                foreach (var dateEntry in _tasks)
                {
                    foreach (var task in dateEntry.Value)
                    {
                        // Format: Date|Name|Color|Time
                        lines.Add($"{dateEntry.Key}|{task.Name}|{task.Color}|{task.Time}");
                    }
                }

                File.WriteAllLines(_tasksFilePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tasks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string GetDateKey(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        private string GetCurrentSelectedDateKey()
        {
            return GetDateKey(new DateTime(_currentDate.Year, _currentDate.Month, _selectedDay));
        }

        private void GenerateCalendar()
        {
            CalendarGrid.Children.Clear();

            // Update month/year display
            MonthYearText.Text = _currentDate.ToString("MMMM");

            // Get first day of month and number of days
            var firstDay = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            var startDayOfWeek = (int)firstDay.DayOfWeek;

            // Add empty cells for days before the first day of the month
            for (int i = 0; i < startDayOfWeek; i++)
            {
                var emptyButton = new Button
                {
                    Style = (Style)FindResource("CalendarDayStyle"),
                    Content = "",
                    IsEnabled = false
                };
                CalendarGrid.Children.Add(emptyButton);
            }

            // Add buttons for each day of the month
            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayButton = new Button
                {
                    Style = day == _selectedDay ? (Style)FindResource("SelectedDayStyle") : (Style)FindResource("CalendarDayStyle"),
                    Content = day.ToString(),
                    Tag = day
                };

                dayButton.Click += DayButton_Click;
                CalendarGrid.Children.Add(dayButton);
            }

            // Fill remaining cells
            var totalCells = CalendarGrid.Children.Count;
            var remainingCells = 42 - totalCells; // 6 rows × 7 columns = 42 total cells

            for (int i = 0; i < remainingCells; i++)
            {
                var emptyButton = new Button
                {
                    Style = (Style)FindResource("CalendarDayStyle"),
                    Content = "",
                    IsEnabled = false
                };
                CalendarGrid.Children.Add(emptyButton);
            }
        }

        private void DayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int day)
            {
                _selectedDay = day;
                GenerateCalendar(); // Refresh to update selected day styling
                UpdateTasksDisplay();
            }
        }

        private void UpdateTasksDisplay()
        {
            // Update selected date text
            var selectedDate = new DateTime(_currentDate.Year, _currentDate.Month, _selectedDay);
            SelectedDateText.Text = selectedDate.ToString("dddd d");

            // Clear current tasks display
            TasksPanel.Children.Clear();

            // Get tasks for selected day
            var dateKey = GetCurrentSelectedDateKey();
            var tasksForDay = _tasks.ContainsKey(dateKey) ? _tasks[dateKey] : new List<TaskItem>();

            // Update task count
            TaskCountText.Text = $"{tasksForDay.Count} task{(tasksForDay.Count != 1 ? "s" : "")}";

            // Add task items to display
            foreach (var task in tasksForDay)
            {
                var taskBorder = new Border
                {
                    Style = (Style)FindResource("TaskItemStyle")
                };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                var colorIndicator = new System.Windows.Shapes.Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(task.Color)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var taskContent = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center
                };

                var taskText = new TextBlock
                {
                    Text = task.Name,
                    Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                    FontSize = 14
                };

                var timeText = new TextBlock
                {
                    Text = task.Time,
                    Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                taskContent.Children.Add(taskText);
                taskContent.Children.Add(timeText);

                stackPanel.Children.Add(colorIndicator);
                stackPanel.Children.Add(taskContent);
                taskBorder.Child = stackPanel;

                TasksPanel.Children.Add(taskBorder);
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(-1);
            _selectedDay = 1; // Reset to first day of new month
            GenerateCalendar();
            UpdateTasksDisplay();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            _selectedDay = 1; // Reset to first day of new month
            GenerateCalendar();
            UpdateTasksDisplay();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            TaskNameInput.Text = "";

            // Reset time to 12:00 PM
            HourComboBox.SelectedIndex = 11; // 12
            MinuteComboBox.SelectedIndex = 0; // 00
            AmPmComboBox.SelectedIndex = 1;   // PM

            // Reset color selection
            _selectedColor = "#ea4335";
            ResetColorSelection();

            AddTaskDialog.Visibility = Visibility.Visible;
            TaskNameInput.Focus();
        }

        private void ResetColorSelection()
        {
            // Reset all color options
            RedColorOption.BorderThickness = new Thickness(0);
            OrangeColorOption.BorderThickness = new Thickness(0);
            GreenColorOption.BorderThickness = new Thickness(0);

            // Highlight red as default
            RedColorOption.BorderThickness = new Thickness(2);
            RedColorOption.BorderBrush = Brushes.White;
        }

        private void ColorOption_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string color)
            {
                _selectedColor = color;

                // Reset all borders
                RedColorOption.BorderThickness = new Thickness(0);
                OrangeColorOption.BorderThickness = new Thickness(0);
                GreenColorOption.BorderThickness = new Thickness(0);

                // Highlight selected color
                border.BorderThickness = new Thickness(2);
                border.BorderBrush = Brushes.White;
            }
        }

        private void CancelTask_Click(object sender, RoutedEventArgs e)
        {
            AddTaskDialog.Visibility = Visibility.Collapsed;
        }

        private void SaveTask_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TaskNameInput.Text))
            {
                var dateKey = GetCurrentSelectedDateKey();
                if (!_tasks.ContainsKey(dateKey))
                {
                    _tasks[dateKey] = new List<TaskItem>();
                }

                // Format time
                var hour = HourComboBox.SelectedItem.ToString();
                var minute = MinuteComboBox.SelectedItem.ToString();
                var amPm = AmPmComboBox.SelectedItem.ToString();
                var timeString = $"{hour}:{minute} {amPm}";

                _tasks[dateKey].Add(new TaskItem
                {
                    Name = TaskNameInput.Text.Trim(),
                    Color = _selectedColor,
                    Time = timeString
                });

                SaveTasks(); // Save to file
                UpdateTasksDisplay();
                AddTaskDialog.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveTasks(); // Save tasks when closing the application
            base.OnClosed(e);
        }
    }

    public class TaskItem
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public string Time { get; set; }
    }
}