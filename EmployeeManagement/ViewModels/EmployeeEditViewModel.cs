namespace EmployeeManagement.ViewModels
{
    public class EmployeeEditViewModel :CreateEmployeeViewModel
    {
        public int Id { get; set; }

        public string ExistingPhotoPath { get; set; }

    }
}
