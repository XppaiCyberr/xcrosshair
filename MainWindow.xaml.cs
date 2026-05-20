using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace xcrosshair
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private const int HOTKEY_ID = 9000;
        private const uint WM_HOTKEY = 0x0312;
        private const uint VK_HOME = 0x24;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hwnd;
        private bool _isLoaded = false;

        public MainWindow()
        {
            InitializeComponent();
            PopulateMonitors();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            RegisterHotKey(_hwnd, HOTKEY_ID, 0, VK_HOME);
            
            MoveToMonitor(Screen.PrimaryScreen);
            EnableClickThrough();

            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;
            _isLoaded = true;
            UpdateCrosshairLayout();
        }

        private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == WM_HOTKEY && (int)msg.wParam == HOTKEY_ID)
            {
                ToggleMenu();
                handled = true;
            }
        }

        private void ToggleMenu()
        {
            if (SettingsMenu.Visibility == Visibility.Visible)
            {
                SettingsMenu.Visibility = Visibility.Collapsed;
                EnableClickThrough();
            }
            else
            {
                SettingsMenu.Visibility = Visibility.Visible;
                DisableClickThrough();
                this.Activate();
            }
        }

        private void EnableClickThrough()
        {
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void DisableClickThrough()
        {
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        private void PopulateMonitors()
        {
            var screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                MonitorComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = $"Monitor {i + 1} ({(screens[i].Primary ? "Primary" : "Secondary")})",
                    Tag = screens[i]
                });
            }
            MonitorComboBox.SelectedIndex = 0;
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag is Screen screen)
            {
                MoveToMonitor(screen);
            }
        }

        private void MoveToMonitor(Screen screen)
        {
            this.Left = screen.Bounds.Left;
            this.Top = screen.Bounds.Top;
            this.Width = screen.Bounds.Width;
            this.Height = screen.Bounds.Height;
            if (_isLoaded) UpdateCrosshairLayout();
        }

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsMenu == null) return;
            if (PositionComboBox.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag.ToString() == "TopRight")
                {
                    SettingsMenu.VerticalAlignment = VerticalAlignment.Top;
                    SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                }
                else
                {
                    SettingsMenu.VerticalAlignment = VerticalAlignment.Center;
                    SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                }
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HorizontalLine == null || VerticalLine == null) return;
            if (ColorComboBox.SelectedItem is ComboBoxItem item)
            {
                var colorStr = item.Tag.ToString();
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                HorizontalLine.Fill = new SolidColorBrush(color);
                VerticalLine.Fill = new SolidColorBrush(color);
            }
        }

        private void SettingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            UpdateCrosshairLayout();
        }

        private void UpdateCrosshairLayout()
        {
            if (HorizontalLine == null || VerticalLine == null || CrosshairCanvas == null) return;

            double size = SizeSlider.Value;
            double thickness = ThicknessSlider.Value;

            SizeLabel.Text = $"Size: {(int)size}";
            ThicknessLabel.Text = $"Thickness: {(int)thickness}";

            HorizontalLine.Width = size;
            HorizontalLine.Height = thickness;
            VerticalLine.Width = thickness;
            VerticalLine.Height = size;

            double centerX = this.Width / 2;
            double centerY = this.Height / 2;

            Canvas.SetLeft(HorizontalLine, centerX - (size / 2));
            Canvas.SetTop(HorizontalLine, centerY - (thickness / 2));

            Canvas.SetLeft(VerticalLine, centerX - (thickness / 2));
            Canvas.SetTop(VerticalLine, centerY - (size / 2));
        }

        protected override void OnClosed(EventArgs e)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcher_ThreadFilterMessage;
            base.OnClosed(e);
        }
    }
}
