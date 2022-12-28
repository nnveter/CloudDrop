// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View
{

    public sealed partial class FilesPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static List<int> _selectioneIndex = new List<int>();
        public static List<Border> _selectioneBorder = new List<Border>();
        public static List<string> AllNameFiles = new List<string>();
        public ObservableCollection<Folder> BreadcrumbBarItem;
        public static CollectionViewSource Files1;
        private bool _tap = false;

        GridLength ConstUpRow = new GridLength(83);
        public FilesPage()
        {
            this.InitializeComponent();

            Files1 = Files;
            BreadcrumbBarItem = MainWindow.BreadcrumbBarItem;

            if (MainWindow.BreadcrumbBarItem.Count > 1)
            {
                BackButtonIsEnable(true);
            }

            LoadFilestoGridView();
        }

        public static async void LoadFilestoGridView()
        {
            AllNameFiles = new List<string>();
            _selectioneIndex = new List<int>();
            _selectioneBorder = new List<Border>();

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
                    ContentId = MainWindow.BreadcrumbBarItem[MainWindow.BreadcrumbBarItem.Count - 1].Id
                };
                try
                {
                    response = await client.GetChildrenContentsAsync(request, headers);
                }
                catch (RpcException)
                {
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
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
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
            await DownloadContent(null, token, contents);
            //TODO: сделать диалог выбора места скачаивания
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
            await DownloadContent(content, token);
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
        }

        private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            var items = MainWindow.BreadcrumbBarItem;
            for (int i = items.Count - 1; i >= args.Index + 1; i--)
            {
                items.RemoveAt(i);
            }
            txt.Text = MainWindow.BreadcrumbBarItem[MainWindow.BreadcrumbBarItem.Count - 1].Id.ToString();
            CheckButtonEnable();
            LoadFilestoGridView();
        }

        private async Task<bool> DeleteContent(Content content, string Token)
        {
            return await content.Detete(Token);
        }

        private async Task<bool> DownloadContent(Content content, string Token, List<Content>? multiDownloadsContent = null, string? path = null)
        {
            if (path == null && content != null)
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", content.name);
            }
            if (multiDownloadsContent == null)
            {
                return await new Downloader().Download(content.id, Token, path, content.name);
            }
            else
            {
                Downloader downloader = new Downloader();
                int FilesCount = multiDownloadsContent.Count;
                int thisIterable = 0;
                downloader.DownloadCompleted += IsCompleted =>
                {
                    thisIterable++;
                    if (FilesCount == thisIterable)
                    {
                        MainWindow.UploadBorder1.Visibility = Visibility.Collapsed;
                        MainWindow.FileItems1.Clear();
                    }
                };
                downloader.ProgressChanged += data =>
                {
                    MainWindow.SetLoadingValue(data, MainWindow.FileItems1);
                };
                MainWindow.UploadBorder1.Visibility = Visibility.Visible;
                foreach (var file in multiDownloadsContent)
                {
                    MainWindow.FileItems1.Add(new ViewFileItem() { Name = file.name, Value = 0 });
                }
                foreach (var file in multiDownloadsContent)
                {
                    path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", file.name);
                    await downloader.Download(file.id, Token, path, file.name);
                }
                return true;
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
    }
}
