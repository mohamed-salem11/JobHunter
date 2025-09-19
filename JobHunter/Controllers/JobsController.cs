using JobHunter.Data;
using JobHunter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
namespace JobHunter.Controllers
{
    [Authorize]
    public class JobsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JobHunterContext _context;

        public JobsController(UserManager<ApplicationUser> userManager, JobHunterContext context)
        {

            _userManager = userManager;
            _context = context;

        }

        //get all jobs
        public async Task<IActionResult> AllJobs()
        {
            var user = await _userManager.GetUserAsync(User);
            var jobs = await _context.Jobs.Where(c => c.CreatedById != user.Id).ToListAsync();
            return View(jobs);
        }

        //search with job title
        public async Task<IActionResult> Search(string jobtitle)
        {
            var user = await _userManager.GetUserAsync(User);
            var jobs = _context.Jobs.Where(a => a.JobTitle.Contains(jobtitle) && a.CreatedById != user.Id).ToList();
            return View(jobs);
        }
        // GET: Jobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // GET: Jobs/Create
        public IActionResult Create()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job, IFormFile imageFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();


            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Please upload an image.");
                return View(job);
            }

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            string[] allowedMimeTypes = { "image/jpeg", "image/jpg", "image/png" };

            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension) || !allowedMimeTypes.Contains(imageFile.ContentType.ToLower()))
            {
                ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG files are allowed.");
                return View(job);
            }


            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileName = $"{user.Id}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);


            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }


            job.ImagePath = $"/uploads/{fileName}";
            job.CreatedById = user.Id;

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPostedJobs));
        }

        // get Jobs i have posted
        public async Task<IActionResult> MyPostedJobs()
        {
            var user = await _userManager.GetUserAsync(User);

            var jobs = await _context.Jobs.Where(c => c.CreatedById == user.Id).ToListAsync();
            return View(jobs);
        }

        // GET: Jobs/Apply/5      
        public async Task<IActionResult> Apply(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId, IFormFile resumeFile)
        {
            var user = await _userManager.GetUserAsync(User);
            var job = await _context.Jobs.FindAsync(jobId);


            if (user == null || job == null || resumeFile == null)
            {
                ModelState.AddModelError("resumeFile", "Only PDF files are allowed.");
                return View("Apply", job);
            }

            var fileExtension = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
            var fileMimeType = resumeFile.ContentType.ToLowerInvariant();

            if (fileExtension != ".pdf" || fileMimeType != "application/pdf")
            {
                ModelState.AddModelError("resumeFile", "Only PDF files are allowed.");

                return View("Apply", job);
            }

   
            var existingApplication = await _context.Applications
                                                  .FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id && a.JobId == jobId);

            if (existingApplication != null)
            {
                ModelState.AddModelError(string.Empty, "You have already applied for this job.");
                return View("Apply", job);
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resumes");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = $"{user.Id}{jobId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);

     
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resumeFile.CopyToAsync(stream);
            }

            var jobApplication = new JobApplication
            {
                JobId = jobId,
                ApplicationUserId = user.Id,
                ResumeFilePath = $"/resumes/{fileName}",
                AppliedDate = DateTime.Now
            };

            _context.Applications.Add(jobApplication);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyApplications));
        }

        //My Applications
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            string userid = user.Id;
            var ids = await _context.Applications.Where(a => a.ApplicationUserId == userid).Select(a => a.JobId).ToListAsync();
            var jobs = _context.Jobs.Where(a => ids.Contains(a.Id)).ToList();

            return View(jobs);
        }

        // View Applications

        public async Task<IActionResult> ViewApplications(int id)
        {
            var resumes = await _context.Applications.Where(a => a.JobId == id).Select(a => a.ResumeFilePath).ToListAsync();
            return View(resumes);
        }
        // GET: Jobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }
            return View(job);
        }

        // POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job, IFormFile imageFile)
        {
            if (id != job.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var existingJob = await _context.Jobs.FindAsync(id);
            if (existingJob == null)
                return NotFound();

            ModelState.Remove("imageFile");

            if (imageFile != null && imageFile.Length > 0)
            {
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                string[] allowedMimeTypes = { "image/jpeg", "image/jpg", "image/png" };

                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension) || !allowedMimeTypes.Contains(imageFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG files are allowed.");
                    job.ImagePath = existingJob.ImagePath;
                    return View(job);
                }

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                if (!string.IsNullOrEmpty(existingJob.ImagePath))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingJob.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = $"{user.Id}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                job.ImagePath = $"/uploads/{fileName}";
            }
            else
            {
                job.ImagePath = existingJob.ImagePath;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    job.CreatedById = user.Id;

                    existingJob.JobTitle = job.JobTitle;
                    existingJob.years_of_experience = job.years_of_experience;
                    existingJob.Requirements = job.Requirements;
                    existingJob.ImagePath = job.ImagePath;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(MyPostedJobs));
            }
            return View(job);
        }
        // GET: Jobs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyPostedJobs));
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.Id == id);
        }

        public IActionResult CV()
        {
            return View();

        }
    }
}


