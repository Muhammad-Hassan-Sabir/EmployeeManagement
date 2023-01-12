using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace EmployeeManagement.Controllers
{
    [Authorize(Roles = "Admin")]
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
        public IActionResult AllUsers()
        {
            var allUsers = userManager.Users;
            return View(allUsers);
        }
        [HttpGet]
        public IActionResult AllRoles()
        {
            var allRoles = roleManager.Roles;
            return View(allRoles);
        }
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Not Found User by {id}";
                return View("NotFound");
            }
            var roles = await userManager.GetRolesAsync(user);
            var claims = await userManager.GetClaimsAsync(user);
            EditUserViewModel editUserViewModel = new()
            {
                Id = id,
                UserName = user.UserName,
                Email = user.Email,
                City = user.City,
                Claims = claims.Select(x => x.Value).ToList(),
                Roles = roles
            };
           

            return View(editUserViewModel);
            
        }
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel editUserViewModel)
        {
            var user = await userManager.FindByIdAsync(editUserViewModel.Id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Not Found User by {editUserViewModel.Id}";
                return View("NotFound");
            }
            else
            {
                user.UserName = editUserViewModel.UserName;
                user.Email = editUserViewModel.Email;
                user.City = editUserViewModel.City;
                var result=await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return  RedirectToAction("AllUsers", "Administration");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(editUserViewModel);
                }
            }
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

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Not Found User by {id}";
                return View("NotFound");
            }
            else
            {
               
                    var resDelete = await userManager.DeleteAsync(user);
                    if (resDelete.Succeeded)
                    {
                        return RedirectToAction("AllUsers", "Administration");
                    }
                else
                {
                    foreach (var error in resDelete.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                return View("AllUsers");
              

            }

        }
        [HttpPost]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role==null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {id}";
                return View(@"NotFound");

            }
          
            var result = await roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                return  RedirectToAction("AllRoles");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);

            }
            return View("AllRoles");
        }


    }
}