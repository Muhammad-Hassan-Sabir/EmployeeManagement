using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace EmployeeManagement.Controllers
{
    //[Authorize(Roles = "Admin")]
    [Authorize(Policy = "AdminRolePolicy")]
    public class AdministrationController : Controller
    {
        private readonly Microsoft.AspNetCore.Identity.RoleManager<IdentityRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<AdministrationController> logger;

        public AdministrationController(RoleManager<IdentityRole> roleManager,
                                        UserManager<ApplicationUser> userManager,
                                        ILogger<AdministrationController> logger)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.logger = logger;
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
        public async Task<IActionResult> ManageUserClaims(string id)
        {
            List<UserClaim> userClaims = new List<UserClaim>();
            ApplicationUser? user = await userManager.FindByIdAsync(id);
            if (user is null)
            {
                ViewBag.ErrorMessage = $"Not Found User by id({id})";
                return View("NotFound");
            }
            IList<Claim>? userHasClaims = await userManager.GetClaimsAsync(user);
            foreach (Claim claim in ClaimsStore.AllClaims)
            {
                UserClaim userClaim = new UserClaim()
                {
                    ClaimType = claim.Type,
                    IsSelected = userHasClaims.Any(x => x.Type == claim.Type)
                };
                userClaims.Add(userClaim);
            }
            UserClaimsViewModel userClaimsViewModel = new UserClaimsViewModel()
            {
                UserId = id,
                Claims = userClaims
            };

            return View(userClaimsViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ManageUserRoles(string id)
        {
            List<UserRolesViewModel> userRolesViewModels = new List<UserRolesViewModel>();
            var user = await userManager.FindByIdAsync(id);
            ViewBag.userId = user.Id;
            if (user is null)
            {
                ViewBag.ErrorMessage = $"Not Found User by id({id})";
                return View("NotFound");
            }
            foreach (var role in roleManager.Roles.ToList())
            {
                UserRolesViewModel userRolesViewModel = new()
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = await userManager.IsInRoleAsync(user, role.Name)
                };
                userRolesViewModels.Add(userRolesViewModel);
            }

            return View(userRolesViewModels);
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
                ViewBag.ErrorMessage = $"Not Found User by id({id})";
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
                ViewBag.ErrorMessage = $"Not Found User by id({editUserViewModel.Id})";
                return View("NotFound");
            }
            else
            {
                user.UserName = editUserViewModel.UserName;
                user.Email = editUserViewModel.Email;
                user.City = editUserViewModel.City;
                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("AllUsers", "Administration");
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
                }
                else if (!(model.IsSelected) && (await userManager.IsInRoleAsync(user, role.Name)))
                {
                    result = await userManager.RemoveFromRoleAsync(user, role.Name);
                }
            }

            return RedirectToAction("EditRole", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"Not Found User by id({id})";
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
        [Authorize(policy: "    ")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                ViewBag.ErrorMessage = $"Not Found Role by {id}";
                return View(@"NotFound");
            }
            try
            {
                var result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    return RedirectToAction("AllRoles");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View("AllRoles");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"Error While Deleting Role: {ex}");
                ViewBag.ErrorTitle = $"{role.Name} role is in use";
                ViewBag.ErrorMessage = $"{role.Name} role cannot be deleted as there are users in this role. If you want to delete this role, please remove the users from the role and then try to delete";

                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ManageUserRoles(List<UserRolesViewModel> userRolesViewModels, string id)
        {
            var user = await userManager.FindByIdAsync(id);
            List<string> removeRoles = new List<string>();
            List<string> addRoles = new List<string>();
            if (user is null)
            {
                ViewBag.ErrorMessage = $"Not Found User by id({id})";
                return View("NotFound");
            }
            foreach (var role in userRolesViewModels)
            {
                if (role.IsSelected && !(await userManager.IsInRoleAsync(user, role.RoleName)))
                {
                    addRoles.Add(role.RoleName);
                }
                else if (!(role.IsSelected) && (await userManager.IsInRoleAsync(user, role.RoleName)))
                {
                    removeRoles.Add(role.RoleName);
                }
            }
            if (addRoles.Count > 0)
            {
                IdentityResult? res = await userManager.AddToRolesAsync(user, addRoles);
            }
            if (removeRoles.Count > 0)
            {
                IdentityResult? res = await userManager.RemoveFromRolesAsync(user, removeRoles);
            }

            return RedirectToAction("EditUser", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> ManageUserClaims(UserClaimsViewModel model)
        {
            ApplicationUser? user = await userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                ViewBag.ErrorMessage = $"Not Found User by id({model.UserId})";
                return View("NotFound");
            }

            IList<Claim>? userHasClaims = await userManager.GetClaimsAsync(user);
            List<Claim> removeClaims = new List<Claim>();
            List<Claim> addClaims = new List<Claim>();

            foreach (UserClaim claim in model.Claims)
            {
                if (claim.IsSelected && !(userHasClaims.Any(x => x.Type == claim.ClaimType)))
                {
                    addClaims.Add(new(claim.ClaimType, claim.ClaimType));
                }
                else if (!(claim.IsSelected) && (userHasClaims.Any(x => x.Type == claim.ClaimType)))
                {
                    removeClaims.Add(new(claim.ClaimType, claim.ClaimType));
                }
            }
            if (addClaims.Count > 0)
            {
                IdentityResult? res = await userManager.AddClaimsAsync(user, addClaims);
                if (!res.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot add selected claims to user");
                    return View(model);
                }
            }
            if (removeClaims.Count > 0)
            {
                IdentityResult? res = await userManager.RemoveClaimsAsync(user, removeClaims);
                if (!res.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot remove user existing claims");
                    return View(model);
                }
            }

            return RedirectToAction("EditUser", new { id = model.UserId });
        }
    }
}