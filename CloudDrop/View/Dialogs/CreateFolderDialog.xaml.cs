// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateFolderDialog : ContentDialog
    {
        string ErrorColor = Constants.Red;
        public string FolderName;
        public FolderCreateStatus FolderStatus;
        public CreateFolderDialog()
        {
            this.InitializeComponent();
            IsPrimaryButtonEnabled = false;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string get = FolderNameBox.Text;
            if (!string.IsNullOrEmpty(get) && !string.IsNullOrWhiteSpace(get))
            {
                if (!CheckOnExist(get))
                {
                    FolderName = get;
                    FolderStatus = FolderCreateStatus.OK;
                    return;
                }
                else
                {
                    FolderName = "The name of this folder is already in use";
                    FolderStatus = FolderCreateStatus.NameAlredyExists;
                    return;
                }
            }
            if (string.IsNullOrWhiteSpace(FolderName) || string.IsNullOrEmpty(get))
            {
                FolderName = "The folder name cannot be empty";
                FolderStatus = FolderCreateStatus.NullName;
                return;
            }
        }

        private bool CheckOnExist(string name)
        {
            return MainWindow.PageGetAllNameFiles(MainWindow.OpenPage).Contains(name);
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FolderName = null;
            FolderStatus = FolderCreateStatus.Cancel;
            return;
        }

        private void FolderNameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrEmpty(textBox.Text) && string.IsNullOrWhiteSpace(textBox.Text))
            {
                IsPrimaryButtonEnabled = false;
                return;
            }
            if (CheckOnExist(textBox.Text))
            {
                ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                ErrorTextBlock.Text = "The name of this folder is already in use";
                IsPrimaryButtonEnabled = false;
                return;
            }
            ErrorTextBlock.Text = "";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            IsPrimaryButtonEnabled = true;
            return;
        }
    }

    public enum FolderCreateStatus
    {
        OK = 0,
        NullName = 1,
        NameAlredyExists = 2,
        Cancel = 3,
    }
}
