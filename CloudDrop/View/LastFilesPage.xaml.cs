// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.View.Account;
using CloudDrop.View.Dialogs;
using CloudDrop.View.Features;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using static CloudDrop.ContentsService;
using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View
{

    public sealed partial class LastFilesPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static List<int> _selectioneIndex = new List<int>();
        public static List<Border> _selectioneBorder = new List<Border>();
        public static List<string> AllNameFiles = new List<string>();
        public ObservableCollection<Folder> BreadcrumbBarItem;
        public static CollectionViewSource Files1;

        private bool _tap = false;
        private bool _tapRight = false;

        public static List<Content> DownloadQueue = new List<Content>();
        public static int DownloadIndex = 0;

        GridLength ConstUpRow = new GridLength(83);
        public static ProgressBar ProgressBar1;
        public LastFilesPage()
        {
            this.InitializeComponent();

            Files1 = Files;
            BreadcrumbBarItem = MainWindow.BreadcrumbBarItem;
            ProgressBar1 = ProgressBar;

            if (MainWindow.BreadcrumbBarItem.Count > 1)
            {
                BackButtonIsEnable(true);
            }

            AllNameFiles = new List<string>();
            LoadFilestoGridView();
        }


        public static async void LoadFilestoGridView()
        {
            AllNameFiles = new List<string>();
            _selectioneIndex = new List<int>();
            _selectioneBorder = new List<Border>();

            ProgressBar1.Visibility = Visibility.Visible;

            var projects = new List<FileAr>();
            var newProject = new FileAr();

            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var token = localSettings.Values["JwtToken"] as string;

            var headers = new Metadata();
            headers.Add("authorization", $"Bearer {token}");
            using (var channel = GrpcChannel.ForAddress(Constants.URL))
            {
                ContentsResponse response;
                var client = new ContentsServiceClient(channel);
                var request = new GetChildrenContentsRequest
                {
                    ContentId = MainWindow.BreadcrumbBarItem[MainWindow.BreadcrumbBarItem.Count - 1].Id, 
                    ContentSort = ContentSort.CreatedAt
                };
                try
                {
                    response = await client.GetChildrenContentsAsync(request, headers);
                }
                catch (RpcException ex)
                {
                    ContentDialog ErrorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = ex.Status.Detail,
                        CloseButtonText = "Ok"
                    };
                    ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                    ProgressBar1.Visibility = Visibility.Collapsed;
                    await ErrorDialog.ShowAsync();
                    return;
                }
                var myContentList = response.Children.Select(x => new Content
                {
                    id = x.Id,
                    storageId = x.Storage.Id,
                    contentType = (ContentType)x.ContentType,
                    path = x.Path,
                    name = x.Name,
                    parentId = x.Parent.Id,
                    size = x.Size,
                    CreateAt = AccountPage.UnixTimeStampToDateTime((long)x.CreatedAt).ToString(),
                    //и т.д
                }).ToList();


                foreach (var item in myContentList)
                {
                    AllNameFiles.Add(item.name);
                    newProject.Activities.Add(item);
                }

                projects.Add(newProject);

                Files1.Source = projects;
            }
            ProgressBar1.Visibility = Visibility.Collapsed;
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (!_tapRight)
            {
                var flyout = CommandBarFlyout2;
                var options = new FlyoutShowOptions()
                {
                    Position = e.GetPosition((FrameworkElement)sender),
                    ShowMode = FlyoutShowMode.Standard
                };
                flyout?.ShowAt((FrameworkElement)sender, options);
            }
            _tapRight = false;
        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Border border = sender as Border;
            if (_selectioneBorder.IndexOf(border) == -1)
            {
                AddSelectionElement(border);
            }
            else
            {
                RemoveSelectionElement(border);
            }
            _tap = true;
        }

        private void Border_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Border border = sender as Border;
            Content Data = (Content)border.DataContext;
            RemoveSelectionElement(border);
            _tap = false;
            if (Data.contentType == ContentType.Folder)
            {
                MainWindow.BreadcrumbBarItem.Add(new Folder() { Id = Data.id, Name = Data.name });
                ClearSelection();
                CheckButtonEnable();
                LoadFilestoGridView();
            }
        }
        private void Border_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutShowOptions myOption = new FlyoutShowOptions();
            myOption.ShowMode = false ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
            CommandBarFlyout1.ShowAt((DependencyObject)sender, myOption);
            _tapRight = true;
        }

        public async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            List<Content> contents = new List<Content>();
            foreach (Border item in _selectioneBorder)
            {
                Content content = item.DataContext as Content;
                if (content.contentType != ContentType.Folder)
                {
                    contents.Add(content);
                }
            }
            ////TODO:Не работает очередь скачивания
            MainWindow main = new MainWindow();
            StorageFolder filesPath = await main.SaveFileDialog();
            main.Close();
            ////
            await DownloadContent(null, token, contents, path: filesPath.Path);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            foreach (Border item in _selectioneBorder)
            {
                Content content = (Content)item.DataContext;
                await DeleteContent(content, token);
            }
            ClearSelection();
            LoadFilestoGridView();
            MainWindow.SetStorageUsed();
        }

        private async void DownloadAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;
            ////TODO:Не работает очередь скачивания
            MainWindow main = new MainWindow();
            StorageFolder filesPath = await main.SaveFileDialog();
            main.Close();
            ////
            await DownloadContent(content, token, path: filesPath.Path);
        }

        private async void DeleteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;
            await DeleteContent(content, token);
            ClearSelection();
            LoadFilestoGridView();
            MainWindow.SetStorageUsed();
        }

        private async void AddAppBarButton2_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String Token = localSettings.Values["JwtToken"] as string;

            var status = await MainWindow.CreateFolder(MainWindow.ContentFrame1.XamlRoot, Token);
            if (status == FolderCreateStatus.OK)
            {
                LoadFilestoGridView();
            }
        }

        private void RefreshAppBarButton2_Click(object sender, RoutedEventArgs e)
        {
            LoadFilestoGridView();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            if (CheckButtonEnable())
            {
                MainWindow.BreadcrumbBarItem.RemoveAt(MainWindow.BreadcrumbBarItem.Count - 1);
                CheckButtonEnable();
                LoadFilestoGridView();
            }
        }

        private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            var items = MainWindow.BreadcrumbBarItem;
            for (int i = items.Count - 1; i >= args.Index + 1; i--)
            {
                items.RemoveAt(i);
            }
            CheckButtonEnable();
            LoadFilestoGridView();
        }

        private async Task<bool> DeleteContent(Content content, string Token)
        {
            return await content.Detete(Token);
        }

        private Task<bool> DownloadContent(Content content, string Token, List<Content> multiDownloadsContent = null, string path = null)
        {
            if (multiDownloadsContent == null)
            {
                AddDownloadQueue(content, path);
                return Task.FromResult(true);
            }
            else
            {
                foreach (var file in multiDownloadsContent)
                {
                    if (DownloadQueue.IndexOf(file) == -1)
                    {
                        MainWindow.FileItems1.Add(new ViewFileItem() { Name = file.name, Value = 0 });
                        AddDownloadQueue(file, path);
                    }
                }
                return Task.FromResult(true);
            }
        }

        public async void AddDownloadQueue(Content content, string filesPath = null)
        {
            content.savePath = filesPath;

            if (DownloadQueue.Count == 0)
            {
                Downloader downloader = new Downloader();
                DownloadQueue.Add(content);
                String Token = localSettings.Values["JwtToken"] as string;
                int parentId = BreadcrumbBarItem[BreadcrumbBarItem.Count - 1].Id;

                MainWindow.UploadBorder1.Visibility = Visibility.Visible;

                downloader.ProgressChanged += data =>
                {
                    MainWindow.SetLoadingValue(data, MainWindow.FileItems1);
                };


                while (DownloadIndex < DownloadQueue.Count)
                {
                    if (DownloadQueue[DownloadIndex].savePath == null) {
                        var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", DownloadQueue[DownloadIndex].name);
                        await downloader.Download(DownloadQueue[DownloadIndex].id, Token, path, DownloadQueue[DownloadIndex].name);
                    }
                    else {
                        var path = System.IO.Path.Combine(DownloadQueue[DownloadIndex].savePath, DownloadQueue[DownloadIndex].name);
                        await downloader.Download(DownloadQueue[DownloadIndex].id, Token, path, DownloadQueue[DownloadIndex].name);
                    }
                    DownloadIndex++;
                }
                DownloadQueue.Clear();
                DownloadIndex = 0;
                if (LastFilesPage.DownloadQueue.Count == 0 && FilesPage.DownloadQueue.Count == 0 && MainWindow.UploadQueue.Count == 0)
                {
                    MainWindow.UploadBorder1.Visibility = Visibility.Collapsed;
                    MainWindow.FileItems1.Clear();
                }
            }
            else
            {
                DownloadQueue.Add(content);
            }
        }

        private void AddSelectionElement(Border border)
        {
            var Projects = (List<FileAr>)Files.Source;
            var file = (Models.Content)border.DataContext;

            if (_selectioneBorder.IndexOf(border) == -1)
            {
                _selectioneIndex.Add(Projects[0].Activities.IndexOf(file));
                _selectioneBorder.Add(border);

                border.Background = new SolidColorBrush(Color.FromArgb(23, 255, 255, 255));
                UpRow.Height = ConstUpRow;
                UpRow2.Height = new GridLength(0);

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Элементов выбранно: {_selectioneIndex.Count}";
                }
                else
                {
                    Header.Text = file.name;
                }
            }
        }

        private void RemoveSelectionElement(Border border)
        {
            var Projects = (List<FileAr>)Files.Source;
            var file = (Models.Content)border.DataContext;
            if (_selectioneBorder.IndexOf(border) != -1)
            {
                _selectioneIndex.Remove(Projects[0].Activities.IndexOf(file));
                _selectioneBorder.Remove(border);
                border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                if (_selectioneBorder.Count == 0 || _selectioneIndex.Count == 0)
                {
                    UpRow.Height = new GridLength(0);
                    UpRow2.Height = ConstUpRow;
                    return;
                }

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Элементов выбранно: {_selectioneIndex.Count}";
                }
                else if (_selectioneIndex.Count == 1)
                {
                    Content content = _selectioneBorder[0].DataContext as Content;
                    Header.Text = content.name;
                }
            }
        }

        private void ClearSelection()
        {
            if (!_tap)
            {
                foreach (var item in _selectioneBorder)
                {
                    item.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    UpRow.Height = new GridLength(0);
                    UpRow2.Height = ConstUpRow;
                }
                _selectioneBorder.Clear();
                _selectioneIndex.Clear();
            }
        }

        private void BackButtonIsEnable(bool isEnable)
        {
            if (isEnable)
            {
                BackButton.IsEnabled = true;
                BackButton2.IsEnabled = true;
            }
            else
            {
                BackButton.IsEnabled = false;
                BackButton2.IsEnabled = false;
            }
        }
        private bool CheckButtonEnable()
        {
            if (MainWindow.BreadcrumbBarItem.Count > 1)
            {
                BackButtonIsEnable(true);
                return true;
            }
            else
            {
                BackButtonIsEnable(false);
                return false;
            }
        }

        private void FeaturesAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;
            FeaturesClass featuresClass = new FeaturesClass(content, AllNameFiles);
            featuresClass.OpenFeatures();
        }

    }

    public class FileAr
    {
        public List<Content> Activities { get; set; } = new List<Content>();

    }
    public class Folder
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
