using AMData.Models.CoreModels;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AMCommunication;

internal class Program
{
    private static IConfiguration _config;
    private static AMDevLogger _logger;

    private static async Task Main(string[] args)
    {
        _logger = new AMDevLogger();
        InitializeConfiguration();
        while (true)
        {
            try
            {
                await ProcessCommunicationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }

    private static void InitializeConfiguration()
    {
        _config = (IConfiguration)new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true);
    }

    private static async Task ProcessCommunicationsAsync()
    {
        var options = new DbContextOptionsBuilder<AMCoreData>()
            .UseSqlServer(_config.GetConnectionString("CoreConnectionString"))
            .Options;

        await using var coreData = new AMCoreData(options, _config, _logger);

        while (true)
        {
            var providerCommTask = new List<ProviderCommunicationModel>();
            var clientCommTask = new List<ClientCommunicationModel>();

            await coreData.ExecuteWithRetryAsync(async () =>
            {
                // Run both queries in parallel
                providerCommTask = await coreData.ProviderCommunications
                    .Where(x => x.DeleteDate == null && x.AttemptThree == null && x.SendAfter < DateTime.UtcNow && !x.Sent)
                    .Include(x => x.Provider)
                    .AsNoTracking()
                    .ToListAsync();

                clientCommTask = await coreData.ClientCommunications
                    .Where(x => x.DeleteDate == null && x.AttemptThree == null && x.SendAfter < DateTime.UtcNow && !x.Sent)
                    .Include(x => x.Client)
                    .AsNoTracking()
                    .ToListAsync();
            });

            if (providerCommTask.Count != 0)
            {
                _logger.LogInfo($"Found {providerCommTask.Count} provider communications");
                var emailTasks = providerCommTask
                    .Select(comm => new AMProviderEmail { Communication = comm })
                    .Select(email => SendEmailAsyncHelper(email)) // no need to wrap with Task.Run
                    .ToList();
                var allEmailResults = await Task.WhenAll(emailTasks);
                foreach (var result in allEmailResults) await HandleEmailResultAsync(result, coreData);
            }
            
            if (clientCommTask.Count != 0)
            {
                _logger.LogInfo($"Found {clientCommTask.Count} client communications");
               var smsTasks = clientCommTask
                    .Select(comm => new AMClientSMS { Communication = comm })
                    .Select(sms => SendSmsAsyncHelper(sms))
                    .ToList();
                var allSmsResults = await Task.WhenAll(smsTasks);
                foreach (var result in allSmsResults) await HandleSmsResultAsync(result, coreData);
            }
            
            Thread.Sleep(2000);
        }
    }

    private static async Task<AMProviderEmail> SendEmailAsyncHelper(AMProviderEmail email)
    {
        var subject = "Attention Needed";
        var apiKey = _config["SendGrid:APIKey"];
        var client = new SendGridClient(apiKey);

        var from = new EmailAddress("rodriguez.luis020797@gmail.com", "AM Tech No Reply");
        var to = new EmailAddress("rodriguez.luis020797@gmail.com",
            $"{email.Communication.Provider.FirstName} {email.Communication.Provider.LastName}");
        var plainText = email.Communication.Message;

        var htmlTemplate =
            await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "emailContent.html"));
        var html = htmlTemplate.Replace("#Subject#", subject).Replace("#Body#", plainText);

        var msg = MailHelper.CreateSingleEmail(from, to, $"AM Tech - {subject}!", plainText, html);
        email.Response = await client.SendEmailAsync(msg);

        return email;
    }

    private static async Task HandleEmailResultAsync(AMProviderEmail result, AMCoreData coreData)
    {
        try
        {
            var comm = new ProviderCommunicationModel();

            await coreData.ExecuteWithRetryAsync(async () =>
            {
                comm = await coreData.ProviderCommunications
                    .Where(x => x.ProviderCommunicationId == result.Communication.ProviderCommunicationId)
                    .Include(x => x.Provider)
                    .FirstOrDefaultAsync();
            });
            
            if (comm == null) throw new ArgumentException(nameof(comm));

            if (comm.AttemptOne == null) comm.AttemptOne = DateTime.UtcNow;
            else if (comm.AttemptTwo == null) comm.AttemptTwo = DateTime.UtcNow;
            else if (comm.AttemptThree == null) comm.AttemptThree = DateTime.UtcNow;

            string message;
            if (result.Response.IsSuccessStatusCode)
            {
                comm.Sent = true;
                message =
                    $"Sent communication ID {comm.ProviderCommunicationId} to {comm.Provider.EMail} (Provider ID {comm.Provider.ProviderId})";
                _logger.LogAudit(message);
            }
            else
            {
                message =
                    $"Failed to send communication ID {comm.ProviderCommunicationId} to {comm.Provider.EMail} (Provider ID {comm.Provider.ProviderId})";
                _logger.LogError(message);
            }

            coreData.ExecuteWithRetryAsync(async () =>
            {
                coreData.ProviderCommunications.Update(comm);
                await coreData.SaveChangesAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }

    private static Task<AMClientSMS> SendSmsAsyncHelper(AMClientSMS comm)
    {
        var accountSid = _config["Twilio:SID"];
        var authToken = _config["Twilio:Token"];
        TwilioClient.Init(accountSid, authToken);

        var message = MessageResource.Create(
            new PhoneNumber("+1" + "8777804236"),
            from: new PhoneNumber(_config["Twilio:PhoneNumber"]),
            body: comm.Communication.Message
        );
        comm.Communication = comm.Communication;
        if (message.Status != MessageResource.StatusEnum.Failed) comm.Success = true;
        return Task.FromResult(comm);
    }

    private static async Task HandleSmsResultAsync(AMClientSMS result, AMCoreData coreData)
    {
        try
        {
            var comm = new ClientCommunicationModel();
            await coreData.ExecuteWithRetryAsync(async () =>
            {
                comm = await coreData.ClientCommunications
                    .Where(x => x.ClientCommunicationId == result.Communication.ClientCommunicationId)
                    .Include(x => x.Client)
                    .FirstOrDefaultAsync();
            });

            if (comm == null) throw new ArgumentException(nameof(comm));

            if (comm.AttemptOne == null) comm.AttemptOne = DateTime.UtcNow;
            else if (comm.AttemptTwo == null) comm.AttemptTwo = DateTime.UtcNow;
            else if (comm.AttemptThree == null) comm.AttemptThree = DateTime.UtcNow;

            string message;
            if (result.Success)
            {
                comm.Sent = true;
                message =
                    $"Sent communication ID {comm.ClientCommunicationId} to +1{comm.Client.PhoneNumber} (Client ID {comm.Client.ClientId})";
                _logger.LogAudit(message);
            }
            else
            {
                message =
                    $"Failed to send communication ID {comm.ClientCommunicationId} to {comm.Client.PhoneNumber} (Provider ID {comm.Client.ClientId})";
                _logger.LogError(message);
            }

            await coreData.ExecuteWithRetryAsync(async () =>
            {
                coreData.ClientCommunications.Update(comm);
                await coreData.SaveChangesAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
    }
}