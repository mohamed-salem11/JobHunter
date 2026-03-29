using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly string _senderEmail = "mghnam895@gmail.com";
    private readonly string _appPassword = "bmkuwiwefkwykdnk";
    private readonly string _displayName = "JobHunter";  

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential(_senderEmail, _appPassword);
                smtpClient.EnableSsl = true;

                 var fromAddress = new MailAddress(_senderEmail, _displayName);
                var toAddress = new MailAddress(toEmail);

                var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"Email sent successfully from {_displayName} to: {toEmail}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending email: {ex.Message}");
        }
    }
}