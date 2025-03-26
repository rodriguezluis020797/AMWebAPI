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
            var programInstance = new Program();
            await programInstance.SendUserCommunicationAsync();
        }

        public async Task SendUserCommunicationAsync()
        {
            {
                var logger = new AMDevLogger();
                logger.LogInfo("+");

                var config = new ConfigurationManager();
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var optionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
                optionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
                DbContextOptions<AMCoreData> options = optionsBuilder.Options;

                try
                {
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
                            tasks.Add(Task.Run(() => SendEmailAsyncHelper(email)));
                        }

                        results = await Task.WhenAll(tasks);

                        var userComm = new UserCommunicationModel();
                        foreach (var result in results)
                        {
                            userComm = _coreData.UserCommunications
                                    .Where(x => x.CommunicationId == result.Communication.CommunicationId)
                                    .FirstOrDefault();
                            try
                            {
                                if (userComm == null)
                                {
                                    throw new Exception(nameof(userComm));
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

                                if (result.Response.IsSuccessStatusCode)
                                {
                                    userComm.Sent = true;
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
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
                logger.LogInfo("-");
                Console.ReadKey();
            }
        }

        public async Task<AMUserEmail> SendEmailAsyncHelper(AMUserEmail email)
        {
            var apiKey = "a";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("your-email@example.com", "Your Name");
            var to = new EmailAddress(email.Communication.User.EMail);
            var msg = MailHelper.CreateSingleEmail(from, to, string.Empty, email.Communication.Message, email.Communication.Message);

            email.Response = await client.SendEmailAsync(msg);
            return email;
        }

        public class AMUserEmail
        {
            public UserCommunicationModel Communication { get; set; } = new UserCommunicationModel();
            public Response Response { get; set; } = default!;
        }
    }
}
