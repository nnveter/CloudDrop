// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.View;
using CloudDrop.View.Account;
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
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Pickers.Provider;
using Windows.UI;
using WinRT;
using WinUIEx;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop
{
    public sealed partial class MainWindow : WindowEx
    {
        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static Frame ContentFrame1;
        public static Button LastFilesButton1;
        public static Button FileButton1;
        public static Button TrashButton1;
        public static Button AccountButton1;
        public static ColumnDefinition LeftColum1;

        public static Button UploadButton1;
        public static Button CreateButton1;

        public static TextBlock UserName1;
        public static TextBlock TariffName1;
        public static TextBlock NumberSystem1;

        public static TextBlock StorageFreeSpace1;
        public static ProgressBar StorageUsedValue1;
        public static ListView LoadListView1;
        public static Border UploadBorder1;

        public static string OpenPage = "LastFiles";

        public static List<StorageFile> UploadQueue = new List<StorageFile>();
        public static int UploadIndex = 0;

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
            AccountButton1 = AccoutButton;
            LeftColum1 = LeftColum;

            UploadButton1 = UploadButton;
            CreateButton1 = CreateButton;

            UserName1 = UserName;
            TariffName1 = TariffName;
            NumberSystem1 = NumberSystem;

            StorageFreeSpace1 = StorageFreeSpace;
            StorageUsedValue1 = StorageUsedValue;
            LoadListView1 = LoadListView;
            UploadBorder1 = UploadBorder;
            FileItems1 = FileItems;

            TrySetMicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            TaskBarIcon = Icon.FromFile("Assets\\Icon.ico");
            
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
                        double used = Math.Round((((double)SplashScreenPage.user.Storage.StorageQuote - SplashScreenPage.user.Storage.StorageUsed) / 1024) / 1024, 2);
                        if (used >= 1024)
                        {
                            StorageFreeSpace1.Text = Math.Round(used / 1024, 2).ToString();
                            NumberSystem1.Text = "GB";
                        }
                        else 
                        {
                            StorageFreeSpace1.Text = used.ToString();
                            NumberSystem1.Text = "MB";
                        }
                        StorageUsedValue1.Value = (int)Math.Round((double)SplashScreenPage.user.Storage.StorageUsed / SplashScreenPage.user.Storage.StorageQuote * 100, 0);
                        SubscriptionMessage subscription = await SplashScreenPage.GetUserPlan(Token);
                        TariffName1.Text = subscription.Plan.Name;
                    }
                    catch (Exception)
                    {
                        //TODO ���������� ������
                    }
                }
            }
        }

        private async void CreateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: ������� �������� ������ ���������
            String Token = localSettings.Values["JwtToken"] as string;
            Button button = sender as Button;
            var status = await CreateFolder(button.XamlRoot, Token);
            if (status == FolderCreateStatus.OK)
            {
                PageLoadFilestoGridView(OpenPage);
            }
        }

        public static async Task<FolderCreateStatus> CreateFolder(XamlRoot xamlRoot, string Token) 
        {
            CreateFolderDialog dialog = new CreateFolderDialog();
            dialog.XamlRoot = xamlRoot;
            await dialog.ShowAsync();

            if (dialog.FolderStatus != FolderCreateStatus.OK && dialog.FolderStatus != FolderCreateStatus.Cancel)
            {
                ContentDialog ErrorDialog = new ContentDialog
                {
                    Title = "Create folder error",
                    Content = dialog.FolderName,
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = xamlRoot;
                var res = await ErrorDialog.ShowAsync();
                return dialog.FolderStatus;
            }
            if (dialog.FolderStatus == FolderCreateStatus.OK)
            {
                await new Content().Create(ContentType.Folder, dialog.FolderName, Token, BreadcrumbBarItem[BreadcrumbBarItem.Count - 1].Id);
                return FolderCreateStatus.OK;
            }
            return FolderCreateStatus.Cancel;
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: �������� �����
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
                    if (UploadQueue.Find(V => V.Path == file.Path) == null)
                    {
                        FileItems.Add(new ViewFileItem() { Name = String.Join('.', file.Name.Split('.').SkipLast(1)), Value = 0 });
                        AddDownloadQueue(file);
                    }
                }
            }
        }

        public async Task<StorageFolder> SaveFileDialog() {
            var filePicker = new FolderPicker();

            var hwnd = this.As<IWindowNative>().WindowHandle;

            var initializeWithWindow = filePicker.As<IInitializeWithWindow>();
            initializeWithWindow.Initialize(hwnd);
            return await filePicker.PickSingleFolderAsync();
        }

        public async void AddDownloadQueue(StorageFile file) 
        {
            if (UploadQueue.Count == 0)
            {
                UploadQueue.Add(file);
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

                while (UploadIndex < UploadQueue.Count)
                {
                    await fts.Upload(
                        token: Token,
                        fileName: String.Join('.', UploadQueue[UploadIndex].Name.Split('.').SkipLast(1)),
                        fileType: UploadQueue[UploadIndex].FileType.TrimStart('.'),
                        storageId: SplashScreenPage.user.Storage.Id,
                        uploadingFilePath: UploadQueue[UploadIndex].Path,
                        parentId: parentId);
                    UploadIndex++;
                }
                PageLoadFilestoGridView(OpenPage);
                SetStorageUsed();
                UploadQueue.Clear();
                UploadIndex = 0;
                if (LastFilesPage.DownloadQueue.Count == 0 && FilesPage.DownloadQueue.Count == 0 && UploadQueue.Count == 0)
                {
                    UploadBorder.Visibility = Visibility.Collapsed;
                    FileItems.Clear();
                }
                
            }
            else
            {
                UploadQueue.Add(file);
            }
        }

        public static void SetLoadingValue(KeyValuePair<string, double> keyValuePair, ObservableCollection<ViewFileItem> viewFileItems)
        {
            //TODO: ������� ������� ����� �������� ��������
            ViewFileItem item = viewFileItems.FirstOrDefault(i => i.Name == keyValuePair.Key);
            int index = viewFileItems.IndexOf(item);
            string newValue = keyValuePair.Value.ToString(); // ����� ��������, ������� ����� ����������

            // �������� ������� ������ �� �������
            var listItem = LoadListView1.ContainerFromIndex(index) as ListViewItem;
            // �������� ������� TextBlock, ������� �������� ��������
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
                NavigateToPage((string)button.Tag);
            }
        }

        public static void NavigateToPage(string tagPage)
        {
            // Use a dictionary to map page names to their types
            var pageTypes = new Dictionary<string, Type> {
                    {"SplashScreen", typeof(SplashScreenPage)},
                    {"Registration", typeof(RegistrationPage)},
                    {"SelectTariff", typeof(SelectTariffPage) },
                    {"Account", typeof(AccountPage) },
                    {"Login", typeof(LoginPage)},
                    {"LastFiles", typeof(LastFilesPage)},
                    {"Files", typeof(FilesPage)},
                    {"Trash", typeof(TrashPage)},
            };

            // Use a switch statement to navigate to the appropriate page
            switch (tagPage)
            {
                case "Account":
                case "Trash":
                    UploadButton1.IsEnabled = false;
                    CreateButton1.IsEnabled = false;
                    goto case "default2";

                case "SelectTariff":
                    LeftColum1.Width = new GridLength(0);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new SuppressNavigationTransitionInfo());
                    break;

                case "SplashScreen":
                case "Registration":
                case "Login":
                    LeftColum1.Width = new GridLength(0);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new SuppressNavigationTransitionInfo());
                    OpenPage = tagPage;
                    SetStorageUsed();
                    break;

                case "default2":
                    LeftColum1.Width = new GridLength(240);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new DrillInNavigationTransitionInfo());
                    OpenPage = tagPage;
                    SetActiveButton(tagPage);
                    SetStorageUsed();
                    break;

                default:
                    LeftColum1.Width = new GridLength(240);
                    ContentFrame1.Navigate(pageTypes[tagPage], null, new DrillInNavigationTransitionInfo());
                    OpenPage = tagPage;
                    UploadButton1.IsEnabled = true;
                    CreateButton1.IsEnabled = true;
                    SetActiveButton(tagPage);
                    SetStorageUsed();
                    break;
            }
        }

        private static void SetActiveButton(string tagPage)
        {
            // Set the default background color for all buttons
            var defaultColor = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            AccountButton1.Background = new SolidColorBrush(Color.FromArgb(30, 0, 69, 255));
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
                case "Account": activeButton = AccountButton1; break;
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
