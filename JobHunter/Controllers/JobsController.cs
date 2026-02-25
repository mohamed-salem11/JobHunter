using AutoMapper;
using iTextSharp.text.pdf;
using JobHunter.Data;
using JobHunter.Models;
using JobHunter.ViewModels;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using pdfParser = iTextSharp.text.pdf.parser;
namespace JobHunter.Controllers
{
    public class JobsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JobHunterContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IMapper _mapper;
        public JobsController(UserManager<ApplicationUser> userManager, JobHunterContext context, IWebHostEnvironment env, IMapper mapper)
        {

            _userManager = userManager;
            _context = context;
            _env = env;
            _mapper = mapper;
        }

    
        public async Task<IActionResult> AllJobs()
        {
            var jobs = await _context.Jobs.ToListAsync();
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var appliedJobIds = await _context.Applications
                    .Where(a => a.ApplicationUserId == user.Id)
                    .Select(a => a.JobId)
                    .ToListAsync();

                ViewBag.AppliedJobIds = appliedJobIds;
                ViewBag.CurrentUserId = user.Id;
            }
            else
            {
                ViewBag.AppliedJobIds = new List<int>();
                ViewBag.CurrentUserId = null;
            }

            return View(jobs);
        }

      
        public async Task<IActionResult> Search(string jobtitle)
        {
            var user = await _userManager.GetUserAsync(User);
            var jobs = await _context.Jobs
          .AsNoTracking() 
          .Where(a => a.JobTitle.ToLower().Contains(jobtitle) ||
                      a.Requirements.ToLower().Contains(jobtitle))
          .ToListAsync(); if (user != null)
            {
                var appliedJobIds = await _context.Applications
                    .Where(a => a.ApplicationUserId == user.Id)
                    .Select(a => a.JobId)
                    .ToListAsync();

                ViewBag.AppliedJobIds = appliedJobIds;
                ViewBag.CurrentUserId = user.Id;
            }
            else
            {
                ViewBag.AppliedJobIds = new List<int>();
                ViewBag.CurrentUserId = null;
            }
            return View(jobs);
        }


    
        [Authorize]
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

  
        [Authorize]
        public async Task<IActionResult> MyPostedJobs()
        {
            var user = await _userManager.GetUserAsync(User);

            var jobs = await _context.Jobs.Where(c => c.CreatedById == user.Id).ToListAsync();
            return View(jobs);
        }
 
            [Authorize]  
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
                ResumeFilePath = $"/resumes/{fileName}"
            };

            _context.Applications.Add(jobApplication);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyApplications));
        }
         
        [Authorize]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            string userid = user.Id;
            var ids = await _context.Applications.Where(a => a.ApplicationUserId == userid).Select(a => a.JobId).ToListAsync();
            var jobs = _context.Jobs.Where(a => ids.Contains(a.Id)).ToList();

            return View(jobs);
        }

        public async Task<IActionResult> ViewApplications(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();
            if (job.CreatedById != currentUserId) return Forbid();

            var applications = await _context.Applications
                .Include(a => a.Applicant)
                .Where(a => a.JobId == id)
                .ToListAsync();

            var requirementsText = (job.Requirements ?? "").ToLowerInvariant();
            var reqTokens = ExtractMeaningfulTokens(requirementsText);

      
            var result = _mapper.Map<List<CvScoreItem>>(applications);
 
            foreach (var item in result)
            {
                if (!string.IsNullOrEmpty(item.ResumeFilePath) && reqTokens.Any())
                {
                    try
                    {
                        var fullPath = Path.Combine(_env.WebRootPath, item.ResumeFilePath.TrimStart('/'));
                        var cvText = ReadPdfText(fullPath).ToLowerInvariant();

                        int matched = reqTokens.Count(token => cvText.Contains(token));
                        item.Score = Math.Round(Math.Min(100, 3 * (double)matched / reqTokens.Count * 100), 1);
                    }
                    catch
                    {
                        item.Score = -1;
                    }
                }
            }

            ViewBag.JobTitle = job.JobTitle;
            return View(result);
        }


        private string ReadPdfText(string fullPath)
        {
            var sb = new StringBuilder();
            using (var reader = new iTextSharp.text.pdf.PdfReader(fullPath))
            {
                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    sb.AppendLine(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, i));
                }
            }
            return sb.ToString();
        }

        private List<string> ExtractMeaningfulTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new();

            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "the","and","or","in","of","to","a","an","is","are","for","with","on",
        "at","by","from","as","be","this","that","have","has","will","should",
        "must","can","may","not","no","but","we","our","your","its","you",
        "experience","required","preferred","ability","knowledge","working",
        "strong","good","excellent","proficient","minimum","years","year",
        "month","months","work","job","position","role","team","skills","skill"
    };

            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

           
            foreach (Match m in Regex.Matches(text, @"[a-z#\+\.]{2,}(?:\s+[a-z#\+\.]{2,}){1,2}"))
            {
                var phrase = m.Value.Trim();
                if (phrase.Split(' ').All(w => !stopWords.Contains(w)) && phrase.Length >= 4)
                    tokens.Add(phrase);
            }

 
            foreach (Match m in Regex.Matches(text, @"[a-z#\+][a-z0-9#\+\.\-]{1,}"))
            {
                var word = m.Value.Trim();
                if (!stopWords.Contains(word) && word.Length >= 2)
                    tokens.Add(word);
            }

    
            foreach (Match m in Regex.Matches(text, @"[\u0600-\u06FF]{3,}"))
            {
                var word = m.Value.Trim();
                if (!stopWords.Contains(word))
                    tokens.Add(word);
            }

            return tokens.ToList();
        }


    
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id);
            if (job == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
 
            if (job.CreatedById != currentUserId)
            {
                return Forbid();
            }

            return View(job);
        }

       
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
            var currentUserId = _userManager.GetUserId(User);

            if (job.CreatedById != currentUserId)
            {
                return Forbid();
            }

            return View(job);
        }
 
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


