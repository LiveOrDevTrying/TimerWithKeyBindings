using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace TimerWithKeyBindings
{
    public partial class MainWindow : Window
    {
        private int _elapsedSeconds;
        private string _outputFilePath;
        private string _settingsFilePath;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly string _appDataFolderPath;

        public MainWindow()
        {
            InitializeComponent();

            // Define the base folder path under LocalApplicationData
            _appDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimerWithKeyBindings");

            // Ensure the folder exists
            Directory.CreateDirectory(_appDataFolderPath);

            // Define file paths
            _settingsFilePath = Path.Combine(_appDataFolderPath, "settings.txt");
            _outputFilePath = Path.Combine(_appDataFolderPath, "TimerOutput.txt");

            // Load settings and timer state
            LoadSettings();
            LoadTimerState();

            // Subscribe to global key events
            KeyInterceptor.KeyPressed += OnGlobalKeyPressed;
            KeyInterceptor.StartListening();

            // Subscribe to the window closing event
            Closing += OnWindowClosing;
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                string savedPath = File.ReadAllText(_settingsFilePath);
                if (!string.IsNullOrWhiteSpace(savedPath) && Path.IsPathFullyQualified(savedPath))
                {
                    _outputFilePath = savedPath;
                }
            }

            FilePathTextBox.Text = _outputFilePath;
        }

        private void SaveSettings()
        {
            try
            {
                File.WriteAllText(_settingsFilePath, _outputFilePath);
            }
            catch (Exception ex)
            {
                ShowError($"Error saving settings: {ex.Message}");
            }
        }

        private void LoadTimerState()
        {
            try
            {
                if (File.Exists(_outputFilePath))
                {
                    string content = File.ReadAllText(_outputFilePath);
                    _elapsedSeconds = TimeSpan.TryParse(content, out var savedTime) ? (int)savedTime.TotalSeconds : 0;
                }
                else
                {
                    SaveTimerState();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading timer state: {ex.Message}");
                _elapsedSeconds = 0;
            }

            UpdateTimerDisplay();
        }

        private void SaveTimerState()
        {
            try
            {
                File.WriteAllText(_outputFilePath, TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"hh\:mm\:ss"));
            }
            catch (Exception ex)
            {
                ShowError($"Error saving timer state: {ex.Message}");
            }
        }

        private void StartTimer()
        {
            if (_cancellationTokenSource != null) return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, token);
                        Interlocked.Increment(ref _elapsedSeconds);
                        Dispatcher.Invoke(() =>
                        {
                            UpdateTimerDisplay();
                            SaveTimerState();
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Task was cancelled
                }
            });
        }

        private void StopTimer()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void ResetTimer()
        {
            _elapsedSeconds = 0;
            UpdateTimerDisplay();
            SaveTimerState();
        }

        private void UpdateTimerDisplay()
        {
            TimerTextBlock.Text = TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"hh\:mm\:ss");
        }

        private void ChangeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Select or Specify File",
                FileName = "TimerOutput.txt",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(_outputFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                _outputFilePath = dialog.FileName;
                FilePathTextBox.Text = _outputFilePath;
                SaveSettings();
                SaveTimerState();
            }
        }

        private void CopyPathButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_outputFilePath);
            MessageBox.Show("Path copied to clipboard!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnGlobalKeyPressed(Key key)
        {
            switch (key)
            {
                case Key.D1:
                    StartTimer();
                    break;
                case Key.D2:
                    StopTimer();
                    break;
                case Key.D3:
                    ResetTimer();
                    break;
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop the timer
            StopTimer();

            // Save the timer state and settings
            SaveTimerState();
            SaveSettings();

            // Unsubscribe from global key events
            KeyInterceptor.KeyPressed -= OnGlobalKeyPressed;

            // Stop global key listener
            KeyInterceptor.StopListening();

            // Unsubscribe from window closing event
            Closing -= OnWindowClosing;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) => StartTimer();
        private void StopButton_Click(object sender, RoutedEventArgs e) => StopTimer();
        private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetTimer();

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
