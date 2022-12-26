using System.ComponentModel.DataAnnotations.Schema;

namespace CloudDrop.Models;

public class User
{
    public int id { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public string name { get; set; }
    
}