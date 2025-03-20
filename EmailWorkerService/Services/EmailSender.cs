using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using EmailWorkerService.Models;

namespace EmailWorkerService.Services
{
    public class EmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailBody emailBody)
        {
            if (emailBody == null) throw new ArgumentNullException();

            try
            {
                string MyEmail = Environment.GetEnvironmentVariable("MY_EMAIL");
                string APP_Pass = Environment.GetEnvironmentVariable("APP_PASS");

                Console.WriteLine(MyEmail??"no data");
                Console.WriteLine(APP_Pass);

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(
                    "Smart Survey",
                    MyEmail
                ));
                email.To.Add(new MailboxAddress(emailBody.Name, emailBody.Email));
                email.Subject = GetSubject(emailBody.Template);
                email.Body = new TextPart("html") { Text = GetHtmlEmailBody(emailBody) }; // Using HTML

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    "smtp.gmail.com",
                    587,
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(
                    MyEmail,
                    APP_Pass
                );
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email sent to {emailBody.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email: {ex.Message}");
            }
        }

        private string GetSubject(Template template) => template switch
        {
            Template.Welcome => "🎉 Welcome to Smart Survey!",
            Template.Validation => "🔑 Your Verification Code",
            Template.ForgetPassword => "🔄 Reset Your Password",
            _ => "📢 Smart Survey Notification",
        };

        private string GetHtmlEmailBody(EmailBody emailBody)
        {
            return emailBody.Template switch
            {
                Template.Welcome => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2 style='color:#4CAF50;'>Welcome, {emailBody.Name}!</h2>
                    <p>We're excited to have you on <strong>Smart Survey</strong>. 🎉</p>
                    <p>Start creating and sharing surveys easily.</p>
                    <a href='https://smart-survey.com' 
                       style='background-color:#4CAF50;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block;'>Get Started</a>
                </body>
            </html>",

                Template.Validation => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Verification Code</h2>
                    <p>Hello {emailBody.Name},</p>
                    <p>Your OTP Code is: <strong style='color:#4CAF50;font-size:18px;'>{emailBody.OtpCode}</strong></p>
                    <p>Please use this code to verify your account.</p>
                </body>
            </html>",

                Template.ForgetPassword => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Password Reset Request</h2>
                    <p>Hello {emailBody.Name},</p>
                    <p>Click the button below to reset your password:</p>
                    <a href='{emailBody.URL}' 
                       style='background-color:#ff5733;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block;'>Reset Password</a>
                </body>
            </html>",

                Template.Invite => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>You're Invited to a Quiz!</h2>
                    <p>Click the button below to join the quiz:</p>
                    <a href='{emailBody.Link}' 
                       style='background-color:#008CBA;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block;'>Join Quiz</a>
                </body>
            </html>",

                Template.Remainder => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Survey Reminder</h2>
                    <p>This is a reminder to complete your pending survey.</p>
                    <p><strong>Deadline:</strong> {emailBody.Info}</p>
                </body>
            </html>",

                Template.Quiz => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Quiz Notification</h2>
                    
                    <p>A new quiz is available for you. Click the link below to participate:</p>
                    <a href='{emailBody.Link}' 
                       style='background-color:#28a745;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block;'>Start Quiz</a>
                </body>
            </html>",

                Template.UniqueLink => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Exclusive Survey Link</h2>
                    <p> {emailBody.Info},</p>
                    <p>Your unique survey link is below. This link can only be used once.</p>
                    <a href='{emailBody.Link}' 
                       style='background-color:#FF9800;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;display:inline-block;'>Take Survey</a>
                </body>
            </html>",

                Template.Failure => $@"
            <html>
                <body style='font-family:Arial, sans-serif;'>
                    <h2>Survey Submission Failed</h2>
                    <p>Unfortunately, your survey submission failed due to the following reason:</p>
                    <p><strong>{emailBody.Info}</strong></p>
                    <p>Please try again or contact support if the issue persists.</p>
                </body>
            </html>",

                _ => "<html><body><p>This is a system notification.</p></body></html>"
            };
        }

    }
}
