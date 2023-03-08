using CloudDrop.Helpers;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CloudDrop.Models;

public class Content
{
    public int id { get; set; }
    private string Name { get; set; }
    public int? storageId { get; set; }

    public ContentType contentType { get; set; }
    public string path { get; set; }
    public long? size { get; set; }

    public string type { get; set; }

    public virtual Content parent { get; set; }
    public int? parentId { get; set; }
    public string CreateAt { get; set; }

    public string Icon { get; set; }

    public string name
    {
        get
        {
            return Name;
        }

        set
        {
            Name = value;
            type = value.Split('.').Last().ToString();
            if (contentType == ContentType.Folder)
            {
                Icon = "ms-appx:///IconTypeAssets/Folder.png";
            }
            else
            {
                if (Constants.types.Contains(type))
                {
                    Icon = $"ms-appx:///IconTypeAssets/{type}.png";
                }
                else
                {
                    Icon = "ms-appx:///IconTypeAssets/Unknown.png";
                }
            }
        }
    }

    public async Task<bool> Create(ContentType ContentType, string Name, string Token, int? ParentId = null)
    {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        var client = new ContentsService.ContentsServiceClient(channel);

        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {Token}");
        if (ContentType == ContentType.Folder)
        {
            try
            {
                var request = new NewFolderMessage { Name = Name, ParentId = ParentId };

                var result = client.NewFolder(request, headers);

                name = result.Name;
                id = result.Id;
                contentType = ContentType.Folder;
                path = result.Path;
            }
            catch (RpcException ex)
            {
                ContentDialog ErrorDialog = new ContentDialog
                {
                    Title = "Error".GetLocalized(),
                    Content = ex.Status.Detail,
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                await ErrorDialog.ShowAsync();
                return false;
            }
        }
        else
        {
            //TODO: создание файла
        }

        channel.ShutdownAsync().Wait();
        return true;
    }

    public async Task<bool> Detete(string token, bool? IsFullDetete = false)
    {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        var client = new ContentsService.ContentsServiceClient(channel);

        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {token}");

        try
        {
            var request = new RemoveContentId { ContentId = id, Full = IsFullDetete };

            var result = client.RemoveContent(request, headers);
        }
        catch (RpcException ex)
        {
            ContentDialog ErrorDialog = new ContentDialog
            {
                Title = "Error".GetLocalized(),
                Content = ex.Status.Detail,
                CloseButtonText = "Ok"
            };
            ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
            await ErrorDialog.ShowAsync();
            return false;
        }

        channel.ShutdownAsync().Wait();
        return true;
    }
}

public enum ContentType
{
    File,
    Folder
}