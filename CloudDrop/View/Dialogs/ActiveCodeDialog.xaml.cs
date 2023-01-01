// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static System.Net.Mime.MediaTypeNames;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View.Dialogs
{
    public sealed partial class ActiveCodeDialog : ContentDialog
    {
        public ActiveCodeStatus Statuscode = ActiveCodeStatus.Cancel;
        public string Code;

        public RpcException ExceptionCode;
        public ActiveCodeDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Statuscode = ActiveCodeStatus.Cancel;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Length == 10 && !String.IsNullOrEmpty(txt.Text) && !String.IsNullOrWhiteSpace(txt.Text))
            {
                IsPrimaryButtonEnabled = true;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var token = localSettings.Values["JwtToken"] as string;

            if (int.TryParse(TextBoxCode.Text, out var code))
            {
                var channel = GrpcChannel.ForAddress(Constants.URL);
                var client = new CodesService.CodesServiceClient(channel);

                var headers = new Metadata();
                headers.Add("authorization", $"Bearer {token}");

                try
                {
                    var call = client.Activate(new ActiveCodeMessage { Code = code }, headers);
                    Statuscode = ActiveCodeStatus.Success;
                }
                catch (RpcException ex)
                {
                    ExceptionCode = ex;
                    Statuscode = ActiveCodeStatus.Error;
                }
            }
        }
    }

    public enum ActiveCodeStatus { 
        Error = 0,
        Success = 1,
        Cancel = 2
    }
}
