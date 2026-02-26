using JobHunter.Data;
using JobHunter.Models;
using JobHunter.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JobHunter.Controllers
{
    public class ProfileController : Controller
    {
        private readonly JobHunterContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(JobHunterContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

   
        public IActionResult SetupInterests()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SetupInterests(List<string> selectedInterests)
        {
            var user = await _userManager.GetUserAsync(User);
             
            var interests = selectedInterests
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Take(3)
                .ToList();

            foreach (var interest in interests)
            {
                _context.UserInterests.Add(new UserInterest
                {
                    UserId = user.Id,
                    Interest = interest
                });
            }

            await _context.SaveChangesAsync();          
            return RedirectToAction("SetupInterestsConfirmation");

        }
        public IActionResult SetupInterestsConfirmation()
        {
            return View();
        }

        
        public async Task<IActionResult> EditInterests()
        {
            var user = await _userManager.GetUserAsync(User);

            var interests = _context.UserInterests
                .Where(ui => ui.UserId == user.Id)
                .Select(ui => ui.Interest)
                .ToList();

            return View(interests);
        }

        [HttpPost]
        public async Task<IActionResult> EditInterests(List<string> updatedInterests)
        {
            var user = await _userManager.GetUserAsync(User);

            var existingInterests = _context.UserInterests
                .Where(ui => ui.UserId == user.Id)
                .ToList();

            _context.UserInterests.RemoveRange(existingInterests);

            var interests = updatedInterests
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Take(3)
                .ToList();

            foreach (var interest in interests)
            {
                _context.UserInterests.Add(new UserInterest
                {
                    UserId = user.Id,
                    Interest = interest
                });
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Saved";
            return RedirectToAction("AllJobs", "Jobs"); 

        }


    }

}
