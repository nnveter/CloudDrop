using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace CloudDrop.Models;

public class Content
{
    public int id { get; set; }
    public int? storageId { get; set; }
    
    public ContentType contentType { get; set; }
    public string? path { get; set; }

    private string Name { get; set; }
    public string name { 
        get { 
            return Name; 
        } 

        set {
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

    public string type { get; set; }
    
    public virtual Content? parent { get; set; }
    public int? parentId { get; set; }

    public string Icon { get; set; }

    public bool Create(ContentType ContentType, string Name, string Token, int? ParentId = null)
    {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        var client = new ContentsService.ContentsServiceClient(channel);

        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {Token}");
        try
        {
            var request = new ContentMessage
            {
                Name = Name,
                ContentType = (ContentTypeEnum)ContentType,
                Parent = new ContentMessage { Id = (int)ParentId }
            };

            var result = client.NewContent(request, headers);

            name = result.Name;
            id = result.Id;
            storageId = result.Storage.Id;
            path = result.Path;
            contentType = (ContentType)result.ContentType;
            parentId = result.Parent.Id;
        }
        catch (RpcException)
        { 
            return false;
        }

        channel.ShutdownAsync().Wait();
        return true;
    }

    public bool Detete(string token) {
        var channel = GrpcChannel.ForAddress(Constants.URL);
        var client = new ContentsService.ContentsServiceClient(channel);

        var headers = new Metadata();
        headers.Add("authorization", $"Bearer {token}");

        try
        {
            var request = new RemoveContentId { ContentId = id };

            var result = client.RemoveContent(request, headers);
        }
        catch (RpcException)
        {
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