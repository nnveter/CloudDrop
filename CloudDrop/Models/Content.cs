using System.ComponentModel.DataAnnotations.Schema;

namespace CloudDrop.Models;

public class Content
{
    public int id { get; set; }
    public ContentType contentType { get; set; }
    public string name { get; set; }
    
    public int? storageId { get; set; }

    public string path { get; set; } = "";

    public string Icon
    {
        get
        {
            if (contentType == ContentType.Folder)
            {
                TypeVisability = "Collapsed";
                return "ms-appx:///IconTypeAssets/Folder.png";
            }
            else
            {
                return "ms-appx:///IconTypeAssets/Unknown.png";
            }
        }
    }
    public string TypeVisability { get; set; } = "Visible";
}

public enum ContentType
{
    File = 0,
    Folder
}