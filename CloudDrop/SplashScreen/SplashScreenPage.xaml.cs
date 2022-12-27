// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.View;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.SplashScreen
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreenPage : Page
    {
        public static User user;
        public SplashScreenPage()
        {
            this.InitializeComponent();
            MainVoid();
        }

        public async static Task<bool> MainVoid()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String Token = localSettings.Values["JwtToken"] as string;

            if (await IsCheckedAuthorization(Token))
            {
                MainWindow.BreadcrumbBarItem = new ObservableCollection<Folder> { new Folder { Name = "Home", Id = await GetUserHomeFolderId(Token) }, };
                MainWindow.NavigateToPage("LastFiles");
                return true;
            }
            else
            {
                MainWindow.NavigateToPage("Login");
                return false;
            }
        }

        public static async Task<int> GetUserHomeFolderId(string Token)
        {
            var channel = GrpcChannel.ForAddress(Constants.URL);
            var client = new ContentsService.ContentsServiceClient(channel);

            var headers = new Metadata();
            headers.Add("authorization", $"Bearer {Token}");

            var request = await client.GetSpecialContentIdAsync(new GetSpecialContentIdRequest { SpecialContentEnum = GetSpecialContentIdEnum.Home }, headers);
            return request.ContentId;
        }

        public async static Task<bool> IsCheckedAuthorization(String JwtTocken)
        {
            var headers = new Metadata
            {
                { "authorization", $"Bearer {JwtTocken}" }
            };

            using (var channel = GrpcChannel.ForAddress(Constants.URL))
            {
                var client = new UsersService.UsersServiceClient(channel);
                try
                {
                    user = await client.GetProfileAsync(new UsersEmptyMessage(), headers);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
