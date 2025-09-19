using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobHunter.Models
{
    public class JobApplication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } 
        public int JobId { get; set; }
        public virtual Job Job { get; set; }

        public string ApplicationUserId { get; set; }  
        public virtual ApplicationUser Applicant { get; set; }

        public string ResumeFilePath { get; set; }
        public DateTime AppliedDate { get; set; } 
    }

}
