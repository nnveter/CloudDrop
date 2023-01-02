// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Helpers;
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
        private bool _tapRight = false;

        private string header = "Trushcan".GetLocalized();
        public static ProgressBar ProgressBar1;
        public TrashPage()
        {
            this.InitializeComponent();

            Files1 = Files;
            ProgressBar1 = ProgressBar;

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
                DeletedContentsMessage response;
                var client = new ContentsServiceClient(channel);
                var request = new EmptyGetContentsMessage();
                try
                {
                    response = await client.GetDeletedContentsAsync(request, headers);
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
                var myContentList = response.ContentMessages.Select(x => new Content
                {
                    id = x.Id,
                    storageId = x.Storage.Id,
                    contentType = (ContentType)x.ContentType,
                    path = x.Path,
                    name = x.Name,
                    parentId = x.Parent != null ? x.Parent.Id : null,
                    //è ò.ä
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

                if (_selectioneIndex.Count > 1)
                {
                    DeleteButton.Label = "DeleteItems".GetLocalized();
                    Header.Text = "SelectElements".GetLocalized() + _selectioneIndex.Count;
                    RecoverButton.IsEnabled = true;
                }
                else
                {
                    DeleteButton.Label = "DeleteItem".GetLocalized();
                    Header.Text = file.name;
                    RecoverButton.IsEnabled = true;
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
                    DeleteButton.Label = "ClearTrushcan".GetLocalized();
                    Header.Text = header;
                    RecoverButton.IsEnabled = false;
                    return;
                }

                if (_selectioneIndex.Count > 1)
                {
                    DeleteButton.Label = "DeleteItems".GetLocalized();
                    Header.Text = "SelectElements".GetLocalized() + _selectioneIndex.Count;
                    RecoverButton.IsEnabled = true;
                }
                else if (_selectioneIndex.Count == 1)
                {
                    DeleteButton.Label = "DeleteItem".GetLocalized();
                    Content content = _selectioneBorder[0].DataContext as Content;
                    Header.Text = content.name;
                    RecoverButton.IsEnabled = true;
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
                DeleteButton.Label = "ClearTrushcan".GetLocalized();
                _selectioneBorder.Clear();
                _selectioneIndex.Clear();
                RecoverButton.IsEnabled = false;
            }
        }

        private void Border_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutShowOptions myOption = new FlyoutShowOptions();
            myOption.ShowMode = false ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
            CommandBarFlyout1.ShowAt((DependencyObject)sender, myOption);
            _tapRight = true;

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

        public async void DeleteContent(string Token, Content content = null)
        {
            if (content == null)
            {
                ClearTrashDialog dialog = new ClearTrashDialog();
                dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                dialog.Text.Text = "DeleteAllItems".GetLocalized();
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

        private void RefreshAppBarButton2_Click(object sender, RoutedEventArgs e)
        {
            LoadFilestoGridView();
        }

        private async void DeleteAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            AppBarButton button = (AppBarButton)sender;
            Content content = (Content)button.DataContext;

            ClearTrashDialog dialog = new ClearTrashDialog();
            dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;

            dialog.Text.Text = "ElementDeleted".GetLocalized();
            dialog.Title = "DeleteFile".GetLocalized();
            var result = await dialog.ShowAsync();
            if (dialog.Result)
            {
                DeleteContent(token, content);
                ClearSelection();
                LoadFilestoGridView();
                MainWindow.SetStorageUsed();
            }
        }

        private async void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            var token = localSettings.Values["JwtToken"] as string;
            foreach (Border item in _selectioneBorder)
            {
                Content content = item.DataContext as Content;
                await RecoverContent(token, content);
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
                dialog.Text.Text = "AllElementDeleted".GetLocalized();
                dialog.Title = "DeleteFile".GetLocalized();
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
            else if (_selectioneBorder.Count == 1)
            {
                dialog.Text.Text = "ElementDeleted".GetLocalized();
                dialog.Title = "DeleteFile".GetLocalized();
                var result = await dialog.ShowAsync();
                if (dialog.Result)
                {
                    DeleteContent(token, (Content)_selectioneBorder[0].DataContext);
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
