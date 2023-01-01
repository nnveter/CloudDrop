using CloudDrop.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace clouddrop.Models;
public class UserInfo : User
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}