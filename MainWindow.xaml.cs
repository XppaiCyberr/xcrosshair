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
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

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
            
            // Move to primary monitor initially
            MoveToMonitor(Screen.PrimaryScreen!);
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
            if (_hwnd == IntPtr.Zero) return;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            
            // Force topmost and ensure no activation
            SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void DisableClickThrough()
        {
            if (_hwnd == IntPtr.Zero) return;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            // Remove transparent and noactivate, but keep toolwindow and topmost
            SetWindowLong(_hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_TRANSPARENT) & ~WS_EX_NOACTIVATE);
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
            if (_hwnd == IntPtr.Zero)
            {
                // Fallback for before window is loaded
                this.Left = screen.Bounds.Left;
                this.Top = screen.Bounds.Top;
                this.Width = screen.Bounds.Width;
                this.Height = screen.Bounds.Height;
                return;
            }

            // Use SetWindowPos for pixel-perfect positioning on the monitor
            SetWindowPos(_hwnd, HWND_TOPMOST, screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

            // Update WPF properties with DPI scaling awareness
            var dpi = VisualTreeHelper.GetDpi(this);
            this.Left = screen.Bounds.Left / dpi.DpiScaleX;
            this.Top = screen.Bounds.Top / dpi.DpiScaleY;
            this.Width = screen.Bounds.Width / dpi.DpiScaleX;
            this.Height = screen.Bounds.Height / dpi.DpiScaleY;

            if (_isLoaded) UpdateCrosshairLayout();
        }

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsMenu == null) return;
            if (PositionComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string position = item.Tag.ToString() ?? "TopRight";
                switch (position)
                {
                    case "TopRight":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Top;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        break;
                    case "TopLeft":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Top;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        break;
                    case "BottomRight":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Bottom;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        break;
                    case "BottomLeft":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Bottom;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        break;
                    case "Center":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Center;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        break;
                }
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HorizontalLine == null || VerticalLine == null) return;
            if (ColorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                var colorStr = item.Tag.ToString() ?? "Lime";
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

            // Use ActualWidth/Height for more accurate centering if available
            double width = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
            double height = this.ActualHeight > 0 ? this.ActualHeight : this.Height;

            if (double.IsNaN(width) || width <= 0) return;

            double centerX = width / 2;
            double centerY = height / 2;

            Canvas.SetLeft(HorizontalLine, centerX - (size / 2));
            Canvas.SetTop(HorizontalLine, centerY - (thickness / 2));

            Canvas.SetLeft(VerticalLine, centerX - (thickness / 2));
            Canvas.SetTop(VerticalLine, centerY - (size / 2));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcher_ThreadFilterMessage;
            base.OnClosed(e);
        }
    }
}
