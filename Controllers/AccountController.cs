using BestStoreMVC.Models;
using BestStoreMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BestStoreMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
        }


        public IActionResult Register()
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
            {
                return View(registerDto);
            }
            // create a new account and authenticate the user
            var user = new ApplicationUser()
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                UserName = registerDto.Email, // UserName will be used to authenticate the user
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                Address = registerDto.Address,
                CreatedAt = DateTime.Now,
            };
            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                // successful user registration
                await userManager.AddToRoleAsync(user, "client");

                // sign in the new user
                await signInManager.SignInAsync(user, false);

                return RedirectToAction("Index", "Home");
            } // registration failed => show registration errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(registerDto);
        }
        public async Task<IActionResult> Logout()
        {
            if (signInManager.IsSignedIn(User))
            {
                await signInManager.SignOutAsync();
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Login()
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(loginDto);
            }

            var result = await signInManager.PasswordSignInAsync(loginDto.Email, loginDto.Password,
                loginDto.RememberMe, false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid login attempt.";
            }

            return View(loginDto);
        }

		[Authorize]
		public async Task<IActionResult> Profile()
		{
			var appUser = await userManager.GetUserAsync(User);
			if (appUser == null)
			{
				return RedirectToAction("Index", "Home");
			}

			var profileDto = new ProfileDto()
			{
				FirstName = appUser.FirstName,
				LastName = appUser.LastName,
				Email = appUser.Email ?? "",
				PhoneNumber = appUser.PhoneNumber,
				Address = appUser.Address,
			};

			return View(profileDto);
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Profile(ProfileDto profileDto)
		{
			if (!ModelState.IsValid)
			{
				ViewBag.ErrorMessage = "Please fill all the required fields with valid values";
				return View(profileDto);
			}

			// Get the current user
			var appUser = await userManager.GetUserAsync(User);
			if (appUser == null)
			{
				return RedirectToAction("Index", "Home");
			}

			// Update the user profile
			appUser.FirstName = profileDto.FirstName;
			appUser.LastName = profileDto.LastName;
			appUser.UserName = profileDto.Email;
			appUser.Email = profileDto.Email;
			appUser.PhoneNumber = profileDto.PhoneNumber;
			appUser.Address = profileDto.Address;

			var result = await userManager.UpdateAsync(appUser);

			if (result.Succeeded)
			{
				ViewBag.SuccessMessage = "Profile updated successfully";
			}
			else
			{
				ViewBag.ErrorMessage = "Unable to update the profile: " + result.Errors.First().Description;
			}


			return View(profileDto);
		}

        [Authorize]
        public IActionResult Password()
        {
            return View();
        }


		[Authorize]
        [HttpPost]
		public async Task<IActionResult> Password(PasswordDto passwordDto)
		{
			if (!ModelState.IsValid)
			{
				return View();
			}

			// Get the current user
			var appUser = await userManager.GetUserAsync(User);
			if (appUser == null)
			{
				return RedirectToAction("Index", "Home");
			}

			// update the password
			var result = await userManager.ChangePasswordAsync(appUser,
				passwordDto.CurrentPassword, passwordDto.NewPassword);

			if (result.Succeeded)
			{
				ViewBag.SuccessMessage = "Password updated successfully!";
			}
			else
			{
				ViewBag.ErrorMessage = "Error: " + result.Errors.First().Description;
			}

			return View();
		}



		public IActionResult AccessDenied()
        {
			return RedirectToAction("Index", "Home");
		}


        public IActionResult ForgotPassword()
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword([Required, EmailAddress] string email)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Email = email;

            if (!ModelState.IsValid)
            {
                ViewBag.EmailError = ModelState["email"]?.Errors.First().ErrorMessage ?? "Invalid Email Address";
                return View();
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user != null)
            {
                // generate password reset token
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                string resetUrl = Url.ActionLink("ResetPassword", "Account", new { token }) ?? "URL Error";

                Console.WriteLine(token);

                //send url by email
                string senderName = configuration["BrevoSettings:SenderName"] ?? "";
                string senderEmail = configuration["BrevoSettings:SenderEmail"] ?? "";
                string username = user.FirstName + " " + user.LastName;
                string subject = "Password Reset";
                string message = "Dear " + username + ",\n\n" +
                                 "You can reset your password using the following link:\n\n" +
                                 resetUrl + "\n\n" +
                                 "Best Regards";

                EmailSender.SendMail(senderName, senderEmail, username, email, subject, message);
            }

            ViewBag.SuccessMessage = "Please check your Email account and click on the Password Reset link!";

            return View();
        }
        public IActionResult ResetPassword(string? token)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");

            }
            if (token == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string? token, PasswordResetDto model)
        {
            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");

            }
            if (token == null)
            {
                return RedirectToAction("Index", "Home");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
           
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) {
                ViewBag.ErrorMessage = "Token not valid!";
                return View(model);            
            }

            var result = await userManager.ResetPasswordAsync(user, token, model.Password);

            if (result.Succeeded) {

                ViewBag.SuccessMessage = "Password reset succesfully";

            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
