using JobHunter.Data;
using JobHunter.Models;
using Microsoft.EntityFrameworkCore;

namespace JobHunter.Services
{
    public class DailyDigestService
    {
        private readonly JobHunterContext _context;
        private readonly EmailService _emailService;

        public DailyDigestService(JobHunterContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task SendDailyDigestAsync()
        {
             var today = DateTime.Today;
            var todayJobs = await _context.Jobs
                .Where(j => j.PostedDate.Date == today)
                .ToListAsync();

            if (!todayJobs.Any()) return;

             var allUserInterests = await _context.UserInterests
                .Include(ui => ui.User)
                .ToListAsync();

             var usersWithInterests = allUserInterests
                .GroupBy(ui => ui.User)
                .ToList();

            foreach (var userGroup in usersWithInterests)
            {
                var user = userGroup.Key;
                var interests = userGroup.Select(ui => ui.Interest.ToLower()).ToList();

                 var matchingJobs = todayJobs.Where(job =>
                    interests.Any(interest =>
                    {
                        var words = interest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var title = job.JobTitle?.ToLower() ?? "";
                        var reqs = job.Requirements?.ToLower() ?? "";

                        return words.Any(word => title.Contains(word) || reqs.Contains(word))
                               || title.Contains(interest)
                               || reqs.Contains(interest);
                    })
                ).ToList();

                if (!matchingJobs.Any()) continue;

                 if (!string.IsNullOrEmpty(user.Email))
                {
                    var emailBody = BuildDigestEmail(user, matchingJobs);
                    await _emailService.SendEmailAsync(
                        user.Email,
                        $"🔔 JobHunter Daily Digest — {matchingJobs.Count} New Job{(matchingJobs.Count > 1 ? "s" : "")} For You!",
                        emailBody
                    );
                }
            }
        }

        private string BuildDigestEmail(ApplicationUser user, List<Job> jobs)
        {
            var jobCards = string.Join("", jobs.Select(job => $@"
                <div style='background: white; border-radius: 10px; padding: 20px;
                            margin: 12px 0; border-left: 4px solid cadetblue;
                            box-shadow: 0 2px 8px rgba(0,0,0,0.06);'>
                    <h3 style='color: cadetblue; margin: 0 0 8px 0;'>{job.JobTitle}</h3>
                    <p style='color: #666; margin: 0; font-size: .9rem;'>
                        {job.Requirements?.Substring(0, Math.Min(job.Requirements.Length, 120))}...
                    </p>
                    <p style='color: #aaa; font-size: .78rem; margin: 8px 0 0 0;'>
                        Posted: {job.PostedDate:MMM dd, yyyy}
                    </p>
                </div>"));

            return $@"
                <div style='font-family: Segoe UI, sans-serif; max-width: 600px; margin: auto; background: #f0f2f5; padding: 20px; border-radius: 16px;'>

                    <!-- Header -->
                    <div style='background: cadetblue; padding: 30px; text-align: center; border-radius: 12px 12px 0 0;'>
                        <h1 style='color: white; margin: 0; font-size: 1.6rem;'>🔔 Daily Job Digest</h1>
                        <p style='color: rgba(255,255,255,0.85); margin: 8px 0 0 0;'>
                            {DateTime.Today:MMMM dd, yyyy}
                        </p>
                    </div>

                    <!-- Body -->
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 12px 12px;'>
                        <p style='font-size: 1rem;'>
                            Hi <strong>{user.FullName ?? user.UserName}</strong>,
                        </p>
                        <p style='color: #555;'>
                            Here are <strong>{jobs.Count} new job{(jobs.Count > 1 ? "s" : "")}</strong>
                            posted today that match your interests:
                        </p>

                        {jobCards}

                        <!-- CTA Button -->
                        <div style='text-align: center; margin-top: 25px;'>
                            <a href='https://jobhunter.runasp.net/Jobs/AllJobs'
                               style='background: cadetblue; color: white; padding: 14px 35px;
                                      border-radius: 50px; text-decoration: none;
                                      font-weight: bold; font-size: 1rem;'>
                                View All Jobs
                            </a>
                        </div>

                        <p style='color: #bbb; font-size: .75rem; margin-top: 30px; text-align: center;'>
                            You received this email because it matches your interests on JobHunter.<br/>
                            You can update your interests anytime from your profile.
                        </p>
                    </div>
                </div>";
        }
    }
}