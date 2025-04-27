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
        private static IConfiguration _config;
        private static AMDevLogger _logger;

        static async Task Main(string[] args)
        {
            _logger = new AMDevLogger();
            _logger.LogInfo("+");

            try
            {
                InitializeConfiguration();
                await ProcessProviderCommunicationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            _logger.LogInfo("-");
        }

        private static void InitializeConfiguration()
        {
            _config = (IConfiguration)new ConfigurationManager()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }

        private static async Task ProcessProviderCommunicationsAsync()
        {
            var options = new DbContextOptionsBuilder<AMCoreData>()
                .UseSqlServer(_config.GetConnectionString("CoreConnectionString"))
                .Options;

            using var _coreData = new AMCoreData(options, _config);

            var communications = await _coreData.ProviderCommunications
                .Where(x => x.DeleteDate == null && x.AttemptThree == null && x.SendAfter < DateTime.UtcNow && !x.Sent)
                .Include(x => x.Provider)
                .AsNoTracking()
                .ToListAsync();

            var emailTasks = communications
                .Select(comm => new AMProviderEmail { Communication = comm })
                .Select(email => Task.Run(() => SendEmailAsyncHelper(email)))
                .ToList();

            var results = await Task.WhenAll(emailTasks);

            foreach (var result in results)
            {
                await HandleEmailResultAsync(result, _coreData);
            }
        }

        private static async Task<AMProviderEmail> SendEmailAsyncHelper(AMProviderEmail email)
        {
            var subject = "Attention Needed";
            var apiKey = _config["SendGrid:APIKey"];
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress("rodriguez.luis020797@gmail.com", "AM Tech No Reply");
            var to = new EmailAddress("rodriguez.luis020797@gmail.com", $"{email.Communication.Provider.FirstName} {email.Communication.Provider.LastName}");
            var plainText = email.Communication.Message;

            var htmlTemplate = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "emailContent.html"));
            var html = htmlTemplate.Replace("#Subject#", subject).Replace("#Body#", plainText);

            var msg = MailHelper.CreateSingleEmail(from, to, $"AM Tech - {subject}!", plainText, html);
            email.Response = await client.SendEmailAsync(msg);

            return email;
        }

        private static async Task HandleEmailResultAsync(AMProviderEmail result, AMCoreData _coreData)
        {
            try
            {
                var comm = await _coreData.ProviderCommunications
                    .Include(x => x.Provider)
                    .FirstOrDefaultAsync(x => x.CommunicationId == result.Communication.CommunicationId);

                if (comm == null) throw new ArgumentException(nameof(comm));

                if (comm.AttemptOne == null) comm.AttemptOne = DateTime.UtcNow;
                else if (comm.AttemptTwo == null) comm.AttemptTwo = DateTime.UtcNow;
                else if (comm.AttemptThree == null) comm.AttemptThree = DateTime.UtcNow;

                string message;
                if (result.Response.IsSuccessStatusCode)
                {
                    comm.Sent = true;
                    message = $"Sent communication ID {comm.CommunicationId} to {comm.Provider.EMail} (Provider ID {comm.Provider.ProviderId})";
                    _logger.LogInfo(message);
                    _logger.LogAudit(message);
                }
                else
                {
                    message = $"Failed to send communication ID {comm.CommunicationId} to {comm.Provider.EMail} (Provider ID {comm.Provider.ProviderId})";
                    _logger.LogError(message);
                }

                _coreData.Update(comm);
                await _coreData.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}