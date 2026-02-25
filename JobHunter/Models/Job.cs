using Microsoft.EntityFrameworkCore;

namespace JobHunter.Models
{
    public class Job
    {
      
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string? CreatedById { get; set; }
        public DateTime PostedDate { get; set; } = DateTime.Now;
        public string Requirements {  get; set; }

        public string ImagePath { get; set; }
       
    }
}
