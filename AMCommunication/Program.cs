using AMData.Models.CoreModels;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AMCommunication
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new AMDevLogger();

            logger.LogInfo("+");
            try
            {
                var programInstance = new Program();
                var config = new ConfigurationManager();
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                await programInstance.SendUserCommunicationAsync(logger, config);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
            logger.LogInfo("-");
        }
        public async Task SendUserCommunicationAsync(AMDevLogger logger, IConfiguration config)
        {
            {
                var optionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
                optionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
                DbContextOptions<AMCoreData> options = optionsBuilder.Options;

                var emailsToSend = new List<AMUserEmail>();
                var tasks = new List<Task<AMUserEmail>>();
                var results = new AMUserEmail[0];

                using (var _coreData = new AMCoreData(options, config))
                {
                    var userComms = _coreData.UserCommunications
                        .Where(x => (x.DeleteDate == null) && (x.AttemptThree == null) && (x.SendAfter < DateTime.UtcNow) && (x.Sent == false))
                        .Include(x => x.User)
                        .AsNoTracking()
                        .ToList();

                    foreach (var comm in userComms)
                    {
                        emailsToSend.Add(new AMUserEmail() { Communication = comm });
                    }

                    foreach (var email in emailsToSend)
                    {
                        tasks.Add(Task.Run(() => SendEmailAsyncHelper(email, config["SendGrid:APIKey"])));
                    }

                    results = await Task.WhenAll(tasks);

                    var userComm = new UserCommunicationModel();
                    foreach (var result in results)
                    {
                        userComm = _coreData.UserCommunications
                                .Where(x => x.CommunicationId == result.Communication.CommunicationId)
                                .Include(x => x.User)
                                .FirstOrDefault();
                        try
                        {
                            if (userComm == null)
                            {
                                throw new ArgumentException(nameof(userComm));
                            }

                            if (userComm.AttemptOne == null)
                            {
                                userComm.AttemptOne = DateTime.UtcNow;
                            }
                            else if (userComm.AttemptTwo == null)
                            {
                                userComm.AttemptTwo = DateTime.UtcNow;
                            }
                            else if (userComm.AttemptThree == null)
                            {
                                userComm.AttemptThree = DateTime.UtcNow;
                            }

                            var str = string.Empty;
                            if (result.Response.IsSuccessStatusCode)
                            {
                                userComm.Sent = true;
                                str = $"Sent user communication id {nameof(userComm.CommunicationId)} to {userComm.User.EMail} with user id {userComm.User.UserId}";
                                logger.LogInfo(str);
                                logger.LogAudit(str);
                            }
                            else
                            {
                                str = $"Unable to send user communication id {nameof(userComm.CommunicationId)} to {userComm.User.EMail} with user id {userComm.User.UserId} - Reason: {result.Response.Body.ReadAsStringAsync()}";
                                logger.LogError(str);
                            }

                            _coreData.Update(userComm);
                            _coreData.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e.ToString());
                        }
                    }
                }

                logger.LogInfo("-");
            }
        }
        public async Task<AMUserEmail> SendEmailAsyncHelper(AMUserEmail email, string apiKey)
        {
            var tempEMailSubject = "Thank you for registering!";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("rodriguez.luis020797@gmail.com", "AM Tech No Reply");
            var subject = $"AM Tech - {tempEMailSubject}!";
            var to = new EmailAddress("rodriguez.luis020797@gmail.com", email.Communication.User.FirstName + " " + email.Communication.User.LastName);
            var plainTextContent = email.Communication.Message;
            var htmlContent = File.ReadAllText(Directory.GetCurrentDirectory() + "/emailContent.html").Replace("#Subject#", tempEMailSubject).Replace("#Body#", email.Communication.Message);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            email.Response = await client.SendEmailAsync(msg);
            return email;
        }
    }
}
