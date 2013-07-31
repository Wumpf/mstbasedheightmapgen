using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        public ProgressBar(Func<bool> closeFunc)
        {
            InitializeComponent();

            Action onIdle = null;
            onIdle = new Action(() =>
            {
                if (closeFunc())
                    Close();
                else
                    this.Dispatcher.BeginInvoke(onIdle, DispatcherPriority.ApplicationIdle);
            });
            this.Dispatcher.BeginInvoke(onIdle, DispatcherPriority.ApplicationIdle);

            this.Loaded += ProgressBar_Loaded;
        }

        void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }


        // http://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
