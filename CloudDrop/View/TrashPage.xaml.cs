// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.View.Dialogs;
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
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using static CloudDrop.ContentsService;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View
{

    public sealed partial class TrashPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static List<int> _selectioneIndex = new List<int>();
        public static List<Border> _selectioneBorder = new List<Border>();
        public static List<string> AllNameFiles = new List<string>();
        public static CollectionViewSource Files1;
        private bool _tap = false;

        private string header = "Trushcan"; //TODO переместить в файл локализации
        public TrashPage()
        {
            this.InitializeComponent();

            Files1 = Files;

            AllNameFiles = new List<string>();
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
                DeletedContentsMessage response;
                var client = new ContentsServiceClient(channel);
                var request = new EmptyGetContentsMessage();
                try
                {
                    response = await client.GetDeletedContentsAsync(request, headers);
                }
                catch (RpcException)
                {
                    return;
                }
                var myContentList = response.ContentMessages.Select(x => new Content
                {
                    id = x.Id,
                    storageId = x.Storage.Id,
                    contentType = (ContentType)x.ContentType,
                    path = x.Path,
                    name = x.Name,
                    parentId = x.Parent != null ? x.Parent.Id : null,
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
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
                Header.Text = header;
                RecoverButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Ёлементов выбранно: {_selectioneIndex.Count}";
                    RecoverButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                }
                else
                {
                    Header.Text = file.name;
                    RecoverButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
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
                    Header.Text = header;
                    RecoverButton.IsEnabled = false;
                    CancelButton.IsEnabled = false;
                    return;
                }

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Ёлементов выбранно: {_selectioneIndex.Count}";
                    RecoverButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                }
                else if (_selectioneIndex.Count == 1)
                {
                    Content content = _selectioneBorder[0].DataContext as Content;
                    Header.Text = content.name;
                    RecoverButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
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
                    Header.Text = header;
                }
                _selectioneBorder.Clear();
                _selectioneIndex.Clear();
                RecoverButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
            }
        }

        private void Border_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutShowOptions myOption = new FlyoutShowOptions();
            myOption.ShowMode = false ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
            CommandBarFlyout1.ShowAt((DependencyObject)sender, myOption);

        }

        public async Task RecoverContent(string Token, Content content)
        {
            try
            {
                using (var channel = GrpcChannel.ForAddress(Constants.URL))
                {
                    var client = new ContentsServiceClient(channel);
                    var request = new RecoveryContentId
                    {
                        ContentId = content.id
                    };

                    var headers = new Metadata();
                    headers.Add("authorization", $"Bearer {Token}");

                    var response = client.RecoveryContent(request, headers);
                }
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
                await ErrorDialog.ShowAsync();
            }
        }

        public async void DeleteContent(string Token, Content? content = null)
        {
            //TODO

            if (content == null)
            {
                ClearTrashDialog dialog = new ClearTrashDialog();
                dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                dialog.Text.Text = "All data is permanently deleted, are you sure you want to empty the trash";
                var result = await dialog.ShowAsync();
                if (dialog.Result)
                {
                    using (var channel = GrpcChannel.ForAddress(Constants.URL))
                    {
                        var client = new ContentsServiceClient(channel);
                        var request = new ContentsEmpty();

                        var headers = new Metadata();
                        headers.Add("authorization", $"Bearer {Token}");

                        var response = client.CleanTrashCan(request, headers);

                        ClearSelection();
                        LoadFilestoGridView();
                        MainWindow.SetStorageUsed();
                    }
                }
                
            }
            else
            {
                await content.Detete(Token, true);
            }
        }


        private async void RecoverAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;
            await RecoverContent(token, content);

            ClearSelection();
            LoadFilestoGridView();
            MainWindow.SetStorageUsed();
        }

        private async void DeleteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;

            ClearTrashDialog dialog = new ClearTrashDialog();
            dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;

            dialog.Text.Text = "This file will be permanently deleted";
            dialog.Title = "Delete file";
            var result = await dialog.ShowAsync();
            if (dialog.Result)
            {
                DeleteContent(token, content);
                ClearSelection();
                LoadFilestoGridView();
                MainWindow.SetStorageUsed();
            }
        }

        private void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            foreach (Border item in _selectioneBorder)
            {
                Content content = item.DataContext as Content;
                RecoverContent(token, content);
            }

            ClearSelection();
            LoadFilestoGridView();
            MainWindow.SetStorageUsed();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;

            ClearTrashDialog dialog = new ClearTrashDialog();
            dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
            if (_selectioneBorder.Count > 1)
            {
                dialog.Text.Text = "The selected files will be permanently deleted";
                dialog.Title = "Delete file";
                var result = await dialog.ShowAsync();
                if (dialog.Result)
                {
                    foreach (Border item in _selectioneBorder)
                    {
                        Content content = (Content)item.DataContext;
                        DeleteContent(token, content);
                    }
                }
            }
            else
            {
                DeleteContent(token);
            }

            if (dialog.Result)
            {
                ClearSelection();
                LoadFilestoGridView();
                MainWindow.SetStorageUsed();
            }
        }
    }
}
