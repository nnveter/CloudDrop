namespace CloudDrop.Models;

public class User
{
    public int id { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string name { get; set; }
    public Storage Storage { get; set; }
    public string country { get; set; }
    public string city { get; set; }

    public static implicit operator User(UserProfileMessage v)
    {
        return new User()
        {
            id = v.Id,
            email = v.Email,
            name = v.Name,
            Storage = new Storage() { Id = v.Storage.Id, StorageQuote = (long)v.Storage.StorageQuote, StorageUsed = v.Storage.StorageUsed },
            
        };
    }
}