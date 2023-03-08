// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.


using CloudDrop.Helpers;
using CloudDrop.Models;
using CloudDrop.View;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
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
        public static PlansMessage plans;
        public static SubscriptionMessage subscription;

        public static TextBlock txt1;
        public SplashScreenPage()
        {
            this.InitializeComponent();
            txt1 = txt;
            _ = MainVoid();
        }

        public async static Task<bool> MainVoid()
        {
            if (!await CheckConnectServer()) {
                return false;
            }

            txt1.Text = "OpenToken".GetLocalized();
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String Token = localSettings.Values["JwtToken"] as string;

            txt1.Text = "CheckAuthorization".GetLocalized();
            if (await IsCheckedAuthorization(Token))
            {
                txt1.Text = "GetUserInfo".GetLocalized();
                MainWindow.BreadcrumbBarItem = new ObservableCollection<Folder> { new Folder { Name = "Home", Id = await GetUserHomeFolderId(Token) }, };
                MainWindow.SetStorageUsed();
                plans = await GetPlans(3);
                subscription = await GetUserPlan(Token);
                MainWindow.UserName1.Text = user.name + " " + user.lastName;
                txt1.Text = "Done".GetLocalized();
                MainWindow.NavigateToPage("LastFiles");
                return true;
            }
            else
            {
                txt1.Text = string.Empty;
                MainWindow.NavigateToPage("Login");
                return false;
            }
        }

        public static async Task<bool> CheckConnectServer() {
            try {
                txt1.Text = "ConnectToServer".GetLocalized();
                var channel = GrpcChannel.ForAddress(Constants.URL);
                var client = new AuthService.AuthServiceClient(channel);

                await client.PingAsync(new PingMessage());
                return true;
            }
            catch {
                txt1.Text = "ErrorConnectBackend".GetLocalized();
                ContentDialog ErrorDialog = new ContentDialog {
                    Title = "Error".GetLocalized(),
                    Content = "ErrorConnectBackend".GetLocalized(),
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = txt1.XamlRoot;
                await ErrorDialog.ShowAsync();
                return false;
            }
        }

        public static async Task<SubscriptionMessage> GetUserPlan(string Token) 
        {
            var headers = new Metadata();
            headers.Add("authorization", $"Bearer {Token}");

            var channel = GrpcChannel.ForAddress(Constants.URL);
            var client = new SubscriptionsService.SubscriptionsServiceClient(channel);
            return await client.GetMySubscriptionAsync(new EmptyMessage(), headers);
            
        }

        public static async Task<PlansMessage> GetPlans(int count) {
            var channel = GrpcChannel.ForAddress(Constants.URL);
            var client = new PlansService.PlansServiceClient(channel);
            PlansMessage request = await client.GetAllAsync(new GetAllRequest() { Max = count });

            return request;
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

        public async static Task<bool> IsCheckedAuthorization(String JwtToken)
        {
            if (JwtToken == null || String.IsNullOrEmpty(JwtToken)) 
                return false;
            var headers = new Metadata
            {
                { "authorization", $"Bearer {JwtToken}" }
            };
            try
            {
                using (var channel = GrpcChannel.ForAddress(Constants.URL))
                {
                    var client = new UsersService.UsersServiceClient(channel);
                    
                    user = await client.GetProfileAsync(new UsersEmptyMessage(), headers);
                    return true;
                    
                    
                }
            } 
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.NotFound) {
                    ContentDialog ErrorDialog = new ContentDialog {
                        Title = "Deauthorization".GetLocalized(),
                        Content = ex.Status.Detail,
                        CloseButtonText = "Ok"
                    };
                    ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                    await ErrorDialog.ShowAsync();

                    ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["JwtToken"] = null;

                    return false;
                }
                return false;
            }
        }
    }
}
