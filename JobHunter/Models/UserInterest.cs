namespace JobHunter.Models
{
    public class UserInterest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public string Interest { get; set; }  
    }

}
