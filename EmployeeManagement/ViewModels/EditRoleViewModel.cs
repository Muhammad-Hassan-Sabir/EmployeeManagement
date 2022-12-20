namespace EmployeeManagement.ViewModels
{
    public class EditRoleViewModel
    {
        public string Id { get; set; }

        public string RoleName { get; set; }

        public List<string> Users { get; set; } = new List<string>();
    }
}