using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Models
{
    public class ApplicationUser:IdentityUser
    {
      
        public string? City { get; set; }
    }
}
