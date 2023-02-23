using EmployeeManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace EmployeeManagement.ViewModels
{
    public class CreateEmployeeViewModel
    {
        [Required, MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Display(Name = "Office Email")]
        [RegularExpression(@"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$", ErrorMessage = "Invalid email format")]
        [Required]
        public string Email { get; set; }

        [Required]
        public Dept? Department { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
