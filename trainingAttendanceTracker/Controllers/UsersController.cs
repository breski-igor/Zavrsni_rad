using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trainingAttendanceTracker.Data;
using trainingAttendanceTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace trainingAttendanceTracker.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Users
        public IActionResult Index()
        {
            var users = _context.Users
                .OrderBy(u => u.Prezime)
                .ThenBy(u => u.Ime)
                .ToList();

            return View(users);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    existingUser.Ime = user.Ime;
                    existingUser.Prezime = user.Prezime;
                    existingUser.DatumRodjenja = user.DatumRodjenja;
                    existingUser.Rang = user.Rang;
                    existingUser.DatumUclanjenja = user.DatumUclanjenja;
                    existingUser.Email = user.Email;
                    existingUser.UserName = user.Email;
                    existingUser.NormalizedEmail = user.Email.ToUpper();
                    existingUser.NormalizedUserName = user.Email.ToUpper();

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Korisnik uspješno ažuriran.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var userAttendance = _context.Evidencija.Where(e => e.UserId == id);
                _context.Evidencija.RemoveRange(userAttendance);

                var userMembershipFees = _context.MembershipFees.Where(m => m.UserId == id);
                _context.MembershipFees.RemoveRange(userMembershipFees);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                var identityUser = await _userManager.FindByIdAsync(id);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                TempData["SuccessMessage"] = "Korisnik uspješno obrisan.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}