// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.View;
using CloudDrop.View.Dialogs;
using CloudDrop.View.Tariff;
using CloudDrop.Views.Autorization;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop
{
    public sealed partial class MainWindow : Window
    {
        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static Frame ContentFrame1;
        public static Button LastFilesButton1;
        public static Button FileButton1;
        public static Button TrashButton1;
        public static ColumnDefinition LeftColum1;

        public static TextBlock StorageFreeSpace1;
        public static ProgressBar StorageUsedValue1;
        public static ListView LoadListView1;
        public static Border UploadBorder1;

        public static string OpenPage = "LastFiles";

        public static List<StorageFile> DownloadQueue = new List<StorageFile>();
        public static int DownloadIndex = 0;

        public ObservableCollection<ViewFileItem> FileItems = new ObservableCollection<ViewFileItem>();
        public static ObservableCollection<ViewFileItem> FileItems1;
        public static ObservableCollection<Folder> BreadcrumbBarItem;

        public MainWindow()
        {
            this.InitializeComponent();

            ContentFrame1 = ContentFrame;
            LastFilesButton1 = LastFilesButton;
            FileButton1 = FileButton;
            TrashButton1 = TrashButton;
            LeftColum1 = LeftColum;

            StorageFreeSpace1 = StorageFreeSpace;
            StorageUsedValue1 = StorageUsedValue;
            LoadListView1 = LoadListView;
            UploadBorder1 = UploadBorder;
            FileItems1 = FileItems;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            TrySetMicaBackdrop();
            NavigateToPage("SplashScreen");
        }

        public static async void SetStorageUsed()
        {
            if (SplashScreenPage.user != null)
            {
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                String Token = localSettings.Values["JwtToken"] as string;
                var headers = new Metadata();
                headers.Add("authorization", $"Bearer {Token}");
                using (var channel = GrpcChannel.ForAddress(Constants.URL))
                {
                    var client = new UsersService.UsersServiceClient(channel);
                    try
                    {
                        UserProfileMessage res = await client.GetProfileAsync(new UsersEmptyMessage(), headers);
                        SplashScreenPage.user = res;
                        StorageFreeSpace1.Text = Math.Round(((double)SplashScreenPage.user.Storage.StorageQuote - SplashScreenPage.user.Storage.StorageUsed) / 1048576, 2).ToString();
                        StorageUsedValue1.Value = (int)Math.Round((double)SplashScreenPage.user.Storage.StorageUsed / SplashScreenPage.user.Storage.StorageQuote * 100, 0);
                    }
                    catch (Exception)
                    {
                        //TODO обработать ошибку
                    }
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO удалить этот метод
        }

        private async void CreateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: сделать создание других элементов
            Button button = sender as Button;
            CreateFolderDialog dialog = new CreateFolderDialog();
            dialog.XamlRoot = button.XamlRoot;
            var result = await dialog.ShowAsync();

            if (dialog.FolderStatus != FolderCreateStatus.OK && dialog.FolderStatus != FolderCreateStatus.Cancel)
            {
                ContentDialog ErrorDialog = new ContentDialog
                {
                    Title = "Create folder error",
                    Content = dialog.FolderName,
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = button.XamlRoot;
                var res = await ErrorDialog.ShowAsync();
                return;
            }
            if (dialog.FolderStatus == FolderCreateStatus.OK)
            {
                String Token = localSettings.Values["JwtToken"] as string;
                await new Content().Create(ContentType.Folder, dialog.FolderName, Token, BreadcrumbBarItem[BreadcrumbBarItem.Count - 1].Id);
                PageLoadFilestoGridView(OpenPage);
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: загрузка папок
            Button button = (Button)sender;
            String Token = localSettings.Values["JwtToken"] as string;

            var filePicker = new FileOpenPicker();

            var hwnd = this.As<IWindowNative>().WindowHandle;

            var initializeWithWindow = filePicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
            filePicker.FileTypeFilter.Add("*");

            var folder = await filePicker.PickMultipleFilesAsync();
            List<StorageFile> arrayFile = folder.ToList();

            int FilesCount = arrayFile.Count;

            if (arrayFile.Count > 0)
            {
                foreach (var file in arrayFile)
                {
                    if (DownloadQueue.Find(V => V.Path == file.Path) == null)
                    {
                        FileItems.Add(new ViewFileItem() { Name = String.Join('.', file.Name.Split('.').SkipLast(1)), Value = 0 });
                        AddDownloadQueue(file);
                    }
                }
            }
        }

        public async void AddDownloadQueue(StorageFile file) 
        {
            if (DownloadQueue.Count == 0)
            {
                DownloadQueue.Add(file);
                String Token = localSettings.Values["JwtToken"] as string;
                int parentId = BreadcrumbBarItem[BreadcrumbBarItem.Count - 1].Id;

                var fts = new FileTransfer(serverUrl: Constants.URL);
                UploadBorder.Visibility = Visibility.Visible;

                fts.MultiPercentOfUpload += data =>
                {
                    SetLoadingValue(data, FileItems);
                };
                fts.UploadError += async ex =>
                {
                    ContentDialog ErrorDialog = new ContentDialog
                    {
                        Title = "Upload file error",
                        Content = ex.Status.Detail,
                        CloseButtonText = "Ok"
                    };
                    ErrorDialog.XamlRoot = ContentFrame.XamlRoot;
                    await ErrorDialog.ShowAsync();
                };

                while (DownloadIndex < DownloadQueue.Count)
                {
                    await fts.Upload(
                        token: Token,
                        fileName: String.Join('.', DownloadQueue[DownloadIndex].Name.Split('.').SkipLast(1)),
                        fileType: DownloadQueue[DownloadIndex].FileType.TrimStart('.'),
                        storageId: SplashScreenPage.user.Storage.Id,
                        uploadingFilePath: DownloadQueue[DownloadIndex].Path,
                        parentId: parentId);
                    DownloadIndex++;
                }
                DownloadQueue.Clear();
                DownloadIndex = 0;
                UploadBorder.Visibility = Visibility.Collapsed;
                FileItems.Clear();
                PageLoadFilestoGridView(OpenPage);
                SetStorageUsed();
            }
            else
            { 
                DownloadQueue.Add(file);
            }
        }

        public static void SetLoadingValue(KeyValuePair<string, double> keyValuePair, ObservableCollection<ViewFileItem> viewFileItems)
        {
            //TODO: сделать плавную смену процента загрузки
            ViewFileItem item = viewFileItems.FirstOrDefault(i => i.Name == keyValuePair.Key);
            int index = viewFileItems.IndexOf(item);
            string newValue = keyValuePair.Value.ToString(); // Новое значение, которое нужно установить

            // Получаем элемент списка по индексу
            var listItem = LoadListView1.ContainerFromIndex(index) as ListViewItem;
            // Получаем элемент TextBlock, который содержит значение
            if (listItem != null)
            {
                item.Value = (int)keyValuePair.Value;
                viewFileItems[index] = item;
            }
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
                    {"SelectTariff", typeof(SelectTariffPage) },
                    {"Login", typeof(LoginPage)},
                    {"LastFiles", typeof(LastFilesPage)},
                    {"Files", typeof(FilesPage)},
                    {"Trash", typeof(TrashPage)},
            };

            // Use a switch statement to navigate to the appropriate page
            switch (tagPage)
            {
                case "SplashScreen":
                case "Registration":
                case "SelectTariff":
                case "Login":
                    LeftColum1.Width = new GridLength(0);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new DrillInNavigationTransitionInfo());
                    OpenPage = tagPage;
                    break;

                default:
                    LeftColum1.Width = new GridLength(240);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new DrillInNavigationTransitionInfo());
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
            TrashButton1.Background = defaultColor;

            // Set the active button's background color
            Button activeButton;
            switch (tagPage)
            {
                case "LastFiles": activeButton = LastFilesButton1; break;
                case "Files": activeButton = FileButton1; break;
                case "Trash": activeButton = TrashButton1; break;
                default: return;
            }
            activeButton.Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
        }

        private void PageLoadFilestoGridView(string tagPage)
        {

            switch (tagPage)
            {
                case "LastFiles":
                    LastFilesPage.LoadFilestoGridView(); break;
                case "Trash":
                    TrashPage.LoadFilestoGridView(); break;
                case "Files":
                    FilesPage.LoadFilestoGridView(); break;
            }
        }

        public static List<string> PageGetAllNameFiles(string tagPage)
        {

            switch (tagPage)
            {
                case "LastFiles":
                    return LastFilesPage.AllNameFiles;
                case "Trash":
                    return TrashPage.AllNameFiles;
                case "Files":
                    return FilesPage.AllNameFiles;
            }
            return new List<string>();
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await SplashScreenPage.MainVoid();
            SetStorageUsed();
        }

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
    }
    public class ViewFileItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
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
