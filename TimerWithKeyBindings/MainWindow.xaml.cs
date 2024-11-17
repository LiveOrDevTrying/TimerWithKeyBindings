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
        private string? _outputFilePath;
        private string _settingsFilePath;
        private CancellationTokenSource? _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();

            // Define settings file path
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.txt");

            // Load the file path from settings or set the default path
            LoadFilePath();

            // Load timer state from file
            LoadTimerState();

            // Start listening for global key presses
            KeyInterceptor.KeyPressed += OnGlobalKeyPressed;
            KeyInterceptor.StartListening();

            Closing += OnWindowClosing;
        }

        private void LoadFilePath()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    _outputFilePath = File.ReadAllText(_settingsFilePath);
                    if (string.IsNullOrWhiteSpace(_outputFilePath) || !Path.IsPathFullyQualified(_outputFilePath))
                    {
                        SetDefaultFilePath();
                    }
                }
                else
                {
                    SetDefaultFilePath();
                }
            }
            catch
            {
                SetDefaultFilePath();
            }

            FilePathTextBox.Text = _outputFilePath;
        }

        private void SetDefaultFilePath()
        {
            _outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimerOutput.txt");
        }

        private void SaveFilePath()
        {
            try
            {
                File.WriteAllText(_settingsFilePath, _outputFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTimerState()
        {
            try
            {
                if (!File.Exists(_outputFilePath))
                {
                    File.WriteAllText(_outputFilePath, "00:00:00");
                }

                string content = File.ReadAllText(_outputFilePath);

                if (TimeSpan.TryParse(content, out TimeSpan savedTime))
                {
                    _elapsedSeconds = (int)savedTime.TotalSeconds;
                }
                else
                {
                    _elapsedSeconds = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading timer state: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _elapsedSeconds = 0;
            }

            UpdateTimerDisplay();
        }

        private void SaveTimerState()
        {
            try
            {
                if (!File.Exists(_outputFilePath))
                {
                    File.Create(_outputFilePath!).Dispose();
                }

                TimeSpan time = TimeSpan.FromSeconds(_elapsedSeconds);
                File.WriteAllText(_outputFilePath!, time.ToString(@"hh\:mm\:ss"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving timer state: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTimer()
        {
            if (_cancellationTokenSource != null)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, token);
                        Interlocked.Increment(ref _elapsedSeconds); // Thread-safe increment
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
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void ResetTimer()
        {
            if (_cancellationTokenSource != null)
            {
                // Timer is running, reset and continue
                Interlocked.Exchange(ref _elapsedSeconds, 0);
                UpdateTimerDisplay();
                SaveTimerState();
            }
            else
            {
                // Timer is stopped, reset but do not start
                Interlocked.Exchange(ref _elapsedSeconds, 0);
                UpdateTimerDisplay();
                SaveTimerState();
            }
        }

        private void UpdateTimerDisplay()
        {
            int elapsedSeconds = Interlocked.CompareExchange(ref _elapsedSeconds, 0, 0); // Thread-safe read
            TimeSpan time = TimeSpan.FromSeconds(elapsedSeconds);
            TimerTextBlock.Text = time.ToString(@"hh\:mm\:ss");
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
                SaveFilePath();
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
            // Respond to Shift + 1, Shift + 2, Shift + 3
            if (key == Key.D1)
            {
                StartTimer();
            }
            else if (key == Key.D2)
            {
                StopTimer();
            }
            else if (key == Key.D3)
            {
                ResetTimer();
            }
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopTimer();
            SaveTimerState();
            SaveFilePath();
            KeyInterceptor.StopListening();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) => StartTimer();
        private void StopButton_Click(object sender, RoutedEventArgs e) => StopTimer();
        private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetTimer();
    }
}
