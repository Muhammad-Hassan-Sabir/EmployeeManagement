using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace EmployeeManagement.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger logger;

        public AccountController(SignInManager<ApplicationUser> signInManager,
                                            UserManager<ApplicationUser> userManager,
                                            ILogger<AccountController> logger)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AcceptVerbs("Get", "Post")]
        [AllowAnonymous]
        public async Task<IActionResult> IsEmailInUse(string email)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return Json(true);
            }
            else
            {
                return Json($"Email {email} is already in use.");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            // If password reset token or email is null, most likely the
            // user tried to tamper the password reset link
            if (email is null || token is null)
            {
                ModelState.AddModelError("", "Invalid password reset token");
            }


            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId is null || token is null)
            {
                return RedirectToAction("index", "home");
            }
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                ViewBag.ErrorMessage = $"The User ID {userId} is invalid";
                return View("NotFound");
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View();
            }
            ViewBag.ErrorTitle = "Email cannot be confirmed";
            return View("Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = registerViewModel.Email, Email = registerViewModel.Email, City = registerViewModel.City };
                var result = await userManager.CreateAsync(user, registerViewModel.Password);
                if (result.Succeeded)
                {
                    string? token = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    string? confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                    logger.LogWarning(confirmationLink);

                    if (signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
                    {
                        return RedirectToAction("AllUsers", "Administration");
                    }
                    //before confirmation email we allow anonymous user to signin
                    //await signInManager.SignInAsync(user, isPersistent: false);
                    //return RedirectToAction("index", "home");

                    ViewBag.ErrorTitle = "Registration successful";
                    ViewBag.ErrorMessage = @"Before you can Login, please confirm your
                                            email, by clicking on the confirmation link we have emailed you";
                    return View("Error");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(registerViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return RedirectToAction("index", "home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            LoginViewModel loginViewModel = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };

            return View(loginViewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            LoginViewModel loginViewModel = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
            if (!(remoteError is null))
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View("Login", loginViewModel);
            }

            // Get the login information about the user from the external login provider
            ExternalLoginInfo? info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                ModelState.AddModelError(string.Empty, "Error loading external login information.");
                return View("Login", loginViewModel);
            }
            // If the user already has a login (i.e if there is a record in AspNetUserLogins
            // table) then sign-in the user with this external login provider
            var res = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
                                                             isPersistent: false, bypassTwoFactor: true);
            if (res.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            // If there is no record in AspNetUserLogins table, the user may not have
            // a local account
            else
            {
                // Get the email claim value
                string? email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (!(email is null))
                {
                    // Create a new user without password if we do not have a user already
                    var user = await userManager.FindByEmailAsync(email);
                    if (user is null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email
                        };
                        await userManager.CreateAsync(user);
                    }
                    // Add a login (i.e insert a row for the user in AspNetUserLogins table)
                    await userManager.AddLoginAsync(user, info);
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                // If we cannot find the user email we cannot continue
                ViewBag.ErrorTitle = $"Email claim not received from: {info.LoginProvider}";
                ViewBag.ErrorMessage = "Please contact support on Pragim@PragimTech.com";
                return View("Error");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            string? redirectUrl = Url.Action("ExternalLoginCallback", "Account",
                                            new { ReturnUrl = returnUrl });
            AuthenticationProperties? properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel, string? returnUrl = null)
        {
            loginViewModel.ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(loginViewModel.Email);

                //checking email is not confirmed
                if (!(user is null) && !user.EmailConfirmed &&
                    (await userManager.CheckPasswordAsync(user, loginViewModel.Password)))
                {
                    ModelState.AddModelError(string.Empty, "Email not confirmed yet");
                    return View(loginViewModel);
                }

                var result = await signInManager.PasswordSignInAsync(loginViewModel.Email, loginViewModel.Password,
                                                                     loginViewModel.RememberMe, false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("index", "home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt");
            }

            return View(loginViewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            if (ModelState.IsValid)
            {
                // Find the user by email
                var user = await userManager.FindByEmailAsync(forgotPasswordViewModel.Email);

                // If the user is found AND Email is confirmed
                if (!(user is null) && (await userManager.IsEmailConfirmedAsync(user)))
                {
                    // Generate the reset password token
                    string? resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

                    // Build the password reset link
                    var resetPasswordLink = Url.Action("ResetPassword", "Account",
                                                  new { email = forgotPasswordViewModel.Email, token = resetToken }, Request.Scheme);

                    // Log the password reset link
                    logger.LogWarning(resetPasswordLink);

                    // Send the user to Forgot Password Confirmation view
                    return View("ForgotPasswordConfirmation");
                }
                // To avoid account enumeration and brute force attacks, don't
                // reveal that the user does not exist or is not confirmed
                return View("ForgotPasswordConfirmation");
            }
            return View(forgotPasswordViewModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Find the user by email
                var user = await userManager.FindByEmailAsync(model.Email);
                if (!(user is null))
                {
                    // reset the user password
                    var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        return View("ResetPasswordConfirmation");
                    }
                    // Display validation errors. For example, password reset token already
                    // used to change the password or password complexity rules not met
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
                // To avoid account enumeration and brute force attacks, don't
                // reveal that the user does not exist
                return View("ResetPasswordConfirmation");
            }

            // Display validation errors if model state is not valid
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}