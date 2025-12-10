using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace trainingAttendanceTracker.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Prezime)
                .ThenBy(u => u.Ime)
                .ToListAsync();

            var userRoles = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    UserName = $"{user.Ime} {user.Prezime}",
                    Email = user.Email,
                    CurrentRole = roles.FirstOrDefault() ?? "Member",
                    Roles = new List<string> { "Admin", "Trainer", "Member" }
                });
            }

            return View(userRoles);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleModel model)
        {
            try
            {
                Console.WriteLine($"Received update request - UserId: {model.UserId}, NewRole: {model.NewRole}");

                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    Console.WriteLine($"User with ID {model.UserId} not found");
                    var userByEmail = await _userManager.FindByEmailAsync(model.UserId);
                    if (userByEmail != null)
                    {
                        Console.WriteLine($"But found user by email: {userByEmail.Email}");
                        user = userByEmail;
                    }
                    else
                    {
                        return Json(new { success = false, error = $"User with ID {model.UserId} not found" });
                    }
                }

                Console.WriteLine($"Found user: {user.UserName} ({user.Email})");

                var currentRoles = await _userManager.GetRolesAsync(user);
                Console.WriteLine($"Current roles: {string.Join(", ", currentRoles)}");

                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                var result = await _userManager.AddToRoleAsync(user, model.NewRole);
                if (result.Succeeded)
                {
                    Console.WriteLine($"Successfully updated role to {model.NewRole}");
                    return Json(new { success = true, message = "Role updated successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Errors: {errors}");
                    return Json(new { success = false, error = errors });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, error = $"Error updating role: {ex.Message}" });
            }
        }

        public class UpdateUserRoleModel
        {
            public string UserId { get; set; }
            public string NewRole { get; set; }
        }

      
    }

    public class UserRoleViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string CurrentRole { get; set; }
        public List<string> Roles { get; set; }
    }
}