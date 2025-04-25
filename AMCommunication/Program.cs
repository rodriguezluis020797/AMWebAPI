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

                await programInstance.SendProviderCommunicationAsync(logger, config);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
            logger.LogInfo("-");
        }
        public async Task SendProviderCommunicationAsync(AMDevLogger logger, IConfiguration config)
        {
            {
                var optionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
                optionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
                DbContextOptions<AMCoreData> options = optionsBuilder.Options;

                var emailsToSend = new List<AMProviderEmail>();
                var tasks = new List<Task<AMProviderEmail>>();
                var results = new AMProviderEmail[0];

                using (var _coreData = new AMCoreData(options, config))
                {
                    var providerComms = _coreData.ProviderCommunications
                        .Where(x => (x.DeleteDate == null) && (x.AttemptThree == null) && (x.SendAfter < DateTime.UtcNow) && (x.Sent == false))
                        .Include(x => x.Provider)
                        .AsNoTracking()
                        .ToList();

                    foreach (var comm in providerComms)
                    {
                        emailsToSend.Add(new AMProviderEmail() { Communication = comm });
                    }

                    foreach (var email in emailsToSend)
                    {
                        tasks.Add(Task.Run(() => SendEmailAsyncHelper(email, config["SendGrid:APIKey"])));
                    }

                    results = await Task.WhenAll(tasks);

                    foreach (var result in results)
                    {
                        var providerComm = _coreData.ProviderCommunications
                                .Where(x => x.CommunicationId == result.Communication.CommunicationId)
                                .Include(x => x.Provider)
                                .FirstOrDefault();
                        try
                        {
                            if (providerComm == null)
                            {
                                throw new ArgumentException(nameof(providerComm));
                            }

                            if (providerComm.AttemptOne == null)
                            {
                                providerComm.AttemptOne = DateTime.UtcNow;
                            }
                            else if (providerComm.AttemptTwo == null)
                            {
                                providerComm.AttemptTwo = DateTime.UtcNow;
                            }
                            else if (providerComm.AttemptThree == null)
                            {
                                providerComm.AttemptThree = DateTime.UtcNow;
                            }

                            var str = string.Empty;
                            if (result.Response.IsSuccessStatusCode)
                            {
                                providerComm.Sent = true;
                                str = $"Sent provider communication id {nameof(providerComm.CommunicationId)} to {providerComm.Provider.EMail} with provider id {providerComm.Provider.ProviderId}";
                                logger.LogInfo(str);
                                logger.LogAudit(str);
                            }
                            else
                            {
                                str = $"Unable to send provider communication id {nameof(providerComm.CommunicationId)} to {providerComm.Provider.EMail} with provider id {providerComm.Provider.ProviderId} - Reason: {result.Response.Body.ReadAsStringAsync()}";
                                logger.LogError(str);
                            }

                            _coreData.Update(providerComm);
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
        public async Task<AMProviderEmail> SendEmailAsyncHelper(AMProviderEmail email, string apiKey)
        {
            var tempEMailSubject = "Thank you for registering!";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("rodriguez.luis020797@gmail.com", "AM Tech No Reply");
            var subject = $"AM Tech - {tempEMailSubject}!";
            var to = new EmailAddress("rodriguez.luis020797@gmail.com", email.Communication.Provider.FirstName + " " + email.Communication.Provider.LastName);
            var plainTextContent = email.Communication.Message;
            var htmlContent = File.ReadAllText(Directory.GetCurrentDirectory() + "/emailContent.html").Replace("#Subject#", tempEMailSubject).Replace("#Body#", email.Communication.Message);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            email.Response = await client.SendEmailAsync(msg);
            return email;
        }
    }
}
