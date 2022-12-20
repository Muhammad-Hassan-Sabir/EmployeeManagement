using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AppDbContext context;

        public AdministrationController(RoleManager<IdentityRole> roleManager,
                                        UserManager<ApplicationUser> userManager,
                                        AppDbContext context)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.context = context;
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel createRoleViewModel)
        {
            if (ModelState.IsValid)
            {
                IdentityRole identityRole = new IdentityRole()
                {
                    Name = createRoleViewModel.RoleName
                };

                var result = await roleManager.CreateAsync(identityRole);
                if (result.Succeeded)
                {
                    return RedirectToAction("AllRoles", "Administration");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(createRoleViewModel);
        }

        [HttpGet]
        public IActionResult AllRoles()
        {
            var allRoles = roleManager.Roles;
            return View(allRoles);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {id}";
                return View("NotFound");
            }
            else
            {
                //var users = .ToList();

                EditRoleViewModel editRoleViewModel = new EditRoleViewModel()
                {
                    Id = role.Id,
                    RoleName = role.Name,
                };
                foreach (var user in userManager.Users)
                {
                    if (await userManager.IsInRoleAsync(user, role.Name))
                    {
                        editRoleViewModel.Users.Add(user.UserName);
                    }
                }

                return View(editRoleViewModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(EditRoleViewModel editRoleViewModel)
        {
            var role = await roleManager.FindByIdAsync(editRoleViewModel.Id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {editRoleViewModel.Id}";
                return View("NotFound");
            }
            role.Name = editRoleViewModel.RoleName;

            var result = await roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                return RedirectToAction("AllRoles", "Administration");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(editRoleViewModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditUsersInRole(string id)
        {
            ViewBag.roleId = id;
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {id}";
                return View("NotFound");
            }

            List<UserRoleViewModel> userRoleViewModels = new List<UserRoleViewModel>();
            var users = userManager.Users.ToList();
            foreach (var user in users)
            {
                UserRoleViewModel model = new UserRoleViewModel()
                {
                    IsSelected = await userManager.IsInRoleAsync(user, role.Name),
                    UserId = user.Id,
                    UserName = user.UserName,
                };
                userRoleViewModels.Add(model);
            }

            return View(userRoleViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> EditUsersInRole(List<UserRoleViewModel> models, string id)
        {
            //List<IdentityUserRole<string>> identityUserRoles = new List<IdentityUserRole<string>>();
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {id}";
                return View("NotFound");
            }
            foreach (var model in models)
            {
                var user = await userManager.FindByIdAsync(model.UserId);
                IdentityResult result = null;
                if (model.IsSelected && !(await userManager.IsInRoleAsync(user, role.Name)))
                {
                    result = await userManager.AddToRoleAsync(user, role.Name);
                    //identityUserRoles.Add(new IdentityUserRole<string> { RoleId = id, UserId = model.UserId });
                }
                else if (!(model.IsSelected) && (await userManager.IsInRoleAsync(user, role.Name)))
                {
                    result = await userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }
            //await context.UserRoles.AddRangeAsync(identityUserRoles);
            //await context.SaveChangesAsync();
            return RedirectToAction("EditRole", new { id = id });
        }
    }
}