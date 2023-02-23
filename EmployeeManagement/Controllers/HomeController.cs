using EmployeeManagement.Models;
using EmployeeManagement.Security;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EmployeeManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment webHostEnvironment;

        // It is through IDataProtector interface Protect and Unprotect methods,
        // we encrypt and decrypt respectively
        private readonly IDataProtector protector;

        // It is the CreateProtector() method of IDataProtectionProvider interface
        // that creates an instance of IDataProtector. CreateProtector() requires
        // a purpose string. So both IDataProtectionProvider and the class that
        // contains our purpose strings are injected using the contructor
        public HomeController(IEmployeeRepository employeeRepository, IWebHostEnvironment webHostEnvironment,
                              IDataProtectionProvider dataProtectionProvider, DataProtectionPurposeStrings dataProtectionPurposeStrings)
        {
            _employeeRepository = employeeRepository;
            this.webHostEnvironment = webHostEnvironment;
            // Pass the purpose string as a parameter
            protector = dataProtectionProvider.CreateProtector(dataProtectionPurposeStrings.EmployeeIdRouteValue);
        }

        [AllowAnonymous]
        public ViewResult Index()
        {
            // retrieve all the employees
            var model = _employeeRepository.GetAllEmployee()
                                        .Select(x =>
                                        {
                                            // Encrypt the ID value and store in EncryptedId property
                                            x.EncryptedId = protector.Protect(Convert.ToString(x.Id));
                                            return x;
                                        });
            // Pass the list of employees to the view
            return View(model);
        }

        // Details view receives the encrypted employee ID
        [AllowAnonymous]
        public ViewResult Details(string id)
        {
            // Decrypt the employee id using Unprotect method
            int? decryptId = Convert.ToInt32(protector.Unprotect(id));
            Employee employee = _employeeRepository.GetEmployee(decryptId ?? 1);
            if (employee is null)
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return View("EmployeeNotFound", decryptId);
            }

            HomeDetailsViewModel homeDetailsViewModel = new HomeDetailsViewModel()
            {
                Employee = employee,
                PageTitle = "Employee Details"
            };

            return View(homeDetailsViewModel);
        }

        [HttpGet]
        public ViewResult Create()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            int decryptId = Convert.ToInt32(protector.Unprotect(id));
            var employee = _employeeRepository.GetEmployee(decryptId);

            EmployeeEditViewModel editViewModel = new EmployeeEditViewModel()
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                ExistingPhotoPath = employee.PhotoPath
            };

            return View(editViewModel);
        }

        [HttpPost]
        public IActionResult Create(CreateEmployeeViewModel employeeModel)
        {
            string uniqueFileName = null;
            if (ModelState.IsValid)
            {
                if (employeeModel.Photo != null)
                {
                    var uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + employeeModel.Photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    employeeModel.Photo.CopyTo(new FileStream(filePath, FileMode.Create));
                }
                Employee newEmployee = new Employee
                {
                    Email = employeeModel.Email,
                    Department = employeeModel.Department,
                    Name = employeeModel.Name,
                    PhotoPath = uniqueFileName
                };
                _employeeRepository.Add(newEmployee);

                return RedirectToAction("details", new { id = newEmployee.Id });
            }
            return View();
        }

        [HttpPost]
        public IActionResult Edit(EmployeeEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                Employee employee = _employeeRepository.GetEmployee(model.Id);
                employee.Name = model.Name;
                employee.Email = model.Email;
                employee.Department = model.Department;

                if (model.Photo != null)
                {
                    if (model.ExistingPhotoPath != null)
                    {
                        string filePath = Path.Combine(webHostEnvironment.WebRootPath,
                                                       "images", model.ExistingPhotoPath);
                        System.IO.File.Delete(filePath);
                    }

                    employee.PhotoPath = ProcessUploadedFile(model.Photo);
                }

                _employeeRepository.Update(employee);
                return RedirectToAction("index");
            }
            return View(model);
        }

        private string ProcessUploadedFile(IFormFile photo)
        {
            string uniqueFileName = null;

            if (photo != null)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    photo.CopyTo(fileStream);
                }
            }

            return uniqueFileName;
        }
    }
}