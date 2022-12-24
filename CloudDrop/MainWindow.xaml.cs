// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.SplashScreen;
using CloudDrop.View;
using CloudDrop.Views.Autorization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI;
using WinRT;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop
{
    public sealed partial class MainWindow : Window
    {
        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        public static Frame ContentFrame1;
        public static Button LastFilesButton1;
        public static Button FileButton1;
        public static Button PhotoButton1;
        public static Button SharedButton1;
        public static Button ArchiveButton1;
        public static Button TrashButton1;
        public static ColumnDefinition LeftColum1;

        public static string OpenPage;

        public MainWindow()
        {
            this.InitializeComponent();

            ContentFrame1 = ContentFrame;
            LastFilesButton1 = LastFilesButton;
            FileButton1 = FileButton;
            PhotoButton1 = PhotoButton;
            SharedButton1 = SharedButton;
            ArchiveButton1 = ArchiveButton;
            TrashButton1 = TrashButton;
            LeftColum1 = LeftColum;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            TrySetMicaBackdrop();
            NavigateToPage("SplashScreen");
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        { }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();

            //Get the Window's HWND
            var hwnd = this.As<IWindowNative>().WindowHandle;

            var initializeWithWindow = filePicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
            filePicker.FileTypeFilter.Add("*");

            var folder = await filePicker.PickMultipleFilesAsync();
            List<StorageFile> arrayFile = folder.ToList();
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if (!button.Tag.Equals(OpenPage))
            {
                OpenPage = (string)button.Tag;
                NavigateToPage(OpenPage);
            }
        }

        public static void NavigateToPage(string tagPage)
        {
            // Use a dictionary to map page names to their types
            var pageTypes = new Dictionary<string, Type> {
                    {"SplashScreen", typeof(SplashScreenPage)},
                    {"Registration", typeof(RegistrationPage)},
                    {"Login", typeof(LoginPage)},
                    {"LastFiles", typeof(LastFilesPage)},
                    {"Files", typeof(FilesPage)},
                    {"Photo", typeof(PhotoPage)},
                    {"Shared", typeof(SharedPage)},
                    {"Archive", typeof(ArchivePage)},
                    {"Trash", typeof(TrashPage)}, 
            };

            // Use a switch statement to navigate to the appropriate page
            switch (tagPage)
            {
                case "SplashScreen": 
                case "Registration":
                case "Login":
                    LeftColum1.Width = new GridLength(0);
                    ContentFrame1.Navigate(pageTypes[tagPage]);
                    OpenPage = tagPage;
                    break;

                default:
                    LeftColum1.Width = new GridLength(240);
                    ContentFrame1.Navigate(pageTypes[tagPage]);
                    OpenPage = tagPage;
                    SetActiveButton(tagPage);
                    break;
            }
        }

        private static void SetActiveButton(string tagPage)
        {
            // Set the default background color for all buttons
            var defaultColor = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            LastFilesButton1.Background = defaultColor;
            FileButton1.Background = defaultColor;
            PhotoButton1.Background = defaultColor;
            SharedButton1.Background = defaultColor;
            ArchiveButton1.Background = defaultColor;
            TrashButton1.Background = defaultColor;

            // Set the active button's background color
            Button activeButton;
            switch (tagPage)
            {
                case "Home": activeButton = LastFilesButton1; break;
                case "Files": activeButton = FileButton1; break;
                case "Photos": activeButton = PhotoButton1; break;
                case "Shared": activeButton = SharedButton1; break;
                case "Archive": activeButton = ArchiveButton1; break;
                case "Trash": activeButton = TrashButton1; break;
                default: return;
            }
            activeButton.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
        }






        //the exit path for initializing the file opening dialog
        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();


        //Thems
        bool TrySetMicaBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }
        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            SplashScreenPage.MainVoid();
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
