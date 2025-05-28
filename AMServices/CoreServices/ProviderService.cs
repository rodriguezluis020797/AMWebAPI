using System.Diagnostics;
using System.Runtime.CompilerServices;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.PaymentEngineServices;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.BillingPortal;

namespace AMServices.CoreServices;

public interface IProviderService
{
    Task<BaseDTO> CreateProviderAsync(ProviderDTO dto);
    Task<ProviderDTO> GetProviderAsync(string jwt, bool generateUrl);
    Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt);
    Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwt);
    Task<BaseDTO> VerifyEMailAsync(string guid, bool verifying);
    Task<BaseDTO> CancelSubscriptionAsync(string jwt);
}

public class ProviderService(
    IAMLogger logger,
    AMCoreData db,
    IConfiguration config,
    IProviderBillingService providerBillingService)
    : IProviderService
{
    public async Task<BaseDTO> CreateProviderAsync(ProviderDTO dto)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        await ExecuteWithRetryAsync(async () =>
        {
            if (await db.Providers.AnyAsync(x => x.EMail.Equals(dto.EMail)))
                response.ErrorMessage = "Provider with given e-mail already exists.\nPlease wait to be given access.";
        });

        if (!string.IsNullOrEmpty(response.ErrorMessage)) return response;

        var provider = new ProviderModel(long.MinValue, dto.FirstName, dto.MiddleName, dto.LastName, dto.EMail,
            dto.AddressLine1, dto.AddressLine2, dto.City, dto.ZipCode, dto.CountryCode, dto.StateCode,
            dto.TimeZoneCode, dto.BusinessName);

        await ExecuteWithRetryAsync(async () =>
        {
            using var trans = await db.Database.BeginTransactionAsync();
            await db.Providers.AddAsync(provider);
            await db.SaveChangesAsync();

            var emailReq = new VerifyProviderEMailRequestModel(provider.ProviderId);
            var message =
                $"Thank you for joining AM Tech!\nPlease verify your email:\n{config["Environment:AngularURI"]}/verify-email?guid={emailReq.QueryGuid}&verifying=true";
            var comm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

            await db.VerifyProviderEMailRequests.AddAsync(emailReq);
            await db.ProviderCommunications.AddAsync(comm);

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        });

        logger.LogAudit($"Provider Id: {provider.ProviderId} - E-Mail: {provider.EMail}");
        return response;
    }

    public async Task<ProviderDTO> GetProviderAsync(string jwt, bool generateUrl)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var provider = new ProviderModel(long.MinValue, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, string.Empty, CountryCodeEnum.Select, StateCodeEnum.Select,
            TimeZoneCodeEnum.Select, string.Empty);

        var service = new SessionService();
        var url = string.Empty;

        await ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers.FirstOrDefaultAsync(u => u.ProviderId == providerId)
                       ?? throw new ArgumentException(nameof(providerId));

            if (generateUrl)
            {
                var options = new SessionCreateOptions
                {
                    Customer = provider.PayEngineId,
                    ReturnUrl = "https://google.com/"
                };

                var session = await service.CreateAsync(options);
                url = session.Url;
            }

            if (provider.DeleteDate != null)
            {
                provider.DeleteDate = null;
                db.Providers.Update(provider);
                await db.SaveChangesAsync();
            }
        });

        var dto = new ProviderDTO();
        dto.CreateNewRecordFromModel(provider, url);
        return dto;
    }

    public async Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt)
    {
        var response = new ProviderDTO();
        var existingProviderExists = false;
        var existingeMailRequestExists = false;
        await ExecuteWithRetryAsync(async () =>
        {
            existingProviderExists = await db.Providers
                .Where(x => x.EMail == dto.EMail)
                .AnyAsync();

            existingeMailRequestExists = await db.UpdateProviderEMailRequests
                .Where(x => x.NewEMail == dto.EMail && x.DeleteDate == null)
                .AnyAsync();
        });
        if (!ValidationTool.IsValidEmail(dto.EMail) || existingProviderExists || existingeMailRequestExists)
        {
            response.ErrorMessage = "Provider with given e-mail already exists or e-mail is not in valid format.";
            return response;
        }


        var providerId = IdentityTool.GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var request = new UpdateProviderEMailRequestModel(providerId, dto.EMail);
        var message =
            $"There has been a request to change your E-Mail.\n" +
            $"If this was not you, please change your password.\n" +
            $"Otherwise, verify here: {config["Environment:AngularURI"]}/verify-email?guid={request.QueryGuid}&verifying=false\n" +
            $"New E-Mail: {request.NewEMail}";

        var communication = new ProviderCommunicationModel(providerId, message, DateTime.MinValue);

        await ExecuteWithRetryAsync(async () =>
        {
            using var trans = await db.Database.BeginTransactionAsync();

            await db.UpdateProviderEMailRequests
                .Where(x => x.ProviderId == providerId)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.UpdateProviderEMailRequests.AddAsync(request);
            await db.ProviderCommunications.AddAsync(communication);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        });

        return response;
    }

    public async Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwt)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var provider = new ProviderModel();

        await ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers.FirstOrDefaultAsync(x => x.ProviderId == providerId)
                       ?? throw new ArgumentException(nameof(providerId));
        });

        provider.UpdateRecordFromDTO(dto);

        var customerService = new CustomerService();
        var options = new CustomerUpdateOptions
        {
            Name = $"{provider.BusinessName} - {provider.FirstName} {provider.LastName}",
            Address = new AddressOptions
            {
                Line1 = provider.AddressLine1,
                Line2 = provider.AddressLine2,
                City = provider.City,
                PostalCode = provider.ZipCode,
                Country = provider.CountryCode.ToString().Replace('_', ' '),
                State = provider.StateCode.ToString().Split('_')[1]
            }
        };

        await ExecuteWithRetryAsync(async () =>
        {
            await customerService.UpdateAsync(provider.PayEngineId, options);
            await db.SaveChangesAsync();
        });
        return response;
    }

    public async Task<BaseDTO> VerifyEMailAsync(string guid, bool verifying)
    {
        var response = new BaseDTO();
        if (verifying)
        {
            var request = new VerifyProviderEMailRequestModel();

            await ExecuteWithRetryAsync(async () =>
            {
                request = await db.VerifyProviderEMailRequests
                    .FirstOrDefaultAsync(x => x.QueryGuid == guid && x.DeleteDate == null);
            });

            if (request == null)
            {
                response.ErrorMessage = "Invalid or expired link.";
                return response;
            }

            var provider = new ProviderModel();

            await ExecuteWithRetryAsync(async () =>
            {
                provider = await db.Providers
                    .FirstOrDefaultAsync(x => x.ProviderId == request.ProviderId);
            });

            var providerPayEngineSessionId = string.Empty;
            var message =
                $"Thank you for verifying your e-mail!\n" +
                $"You will receive an e-mail when you have been given access to the system.";

            var comm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);
            var customerService = new CustomerService();
            var options = new CustomerUpdateOptions
            {
                Name = $"{provider.BusinessName} - {provider.FirstName} {provider.LastName}",
                Email = provider.EMail,
                Address = new AddressOptions
                {
                    Line1 = provider.AddressLine1,
                    Line2 = provider.AddressLine2,
                    City = provider.City,
                    PostalCode = provider.ZipCode,
                    Country = provider.CountryCode.ToString().Replace('_', ' '),
                    State = provider.StateCode.ToString().Split('_')[1]
                }
            };

            await ExecuteWithRetryAsync(async () =>
            {
                using var trans = await db.Database
                    .BeginTransactionAsync();

                var providerPayEngineProfileId = await providerBillingService
                    .CreateProviderBillingProfileAsync(provider.EMail, provider.BusinessName, provider.FirstName,
                        provider.MiddleName, provider.LastName);

                await customerService
                    .UpdateAsync(provider.PayEngineId, options);

                provider.EMailVerified = true;
                provider.PayEngineId = providerPayEngineProfileId;
                provider.UpdateDate = DateTime.UtcNow;
                request.DeleteDate = DateTime.UtcNow;
                db.ProviderCommunications.Add(comm);

                await db.SaveChangesAsync();

                providerPayEngineSessionId =
                    await providerBillingService
                        .CreateProviderSession(providerPayEngineProfileId, "setup");


                await trans.CommitAsync();
            });

            logger.LogAudit(
                $"Email Verified - Provider Id: {provider.ProviderId} - Pay Session Id: {providerPayEngineSessionId}");
        }
        else
        {
            var request = new UpdateProviderEMailRequestModel();

            await ExecuteWithRetryAsync(async () =>
            {
                request = await db.UpdateProviderEMailRequests
                    .FirstOrDefaultAsync(x => x.QueryGuid == guid && x.DeleteDate == null);
            });

            if (request == null)
            {
                response.ErrorMessage = "Invalid or expired link.";
                return response;
            }

            var provider = new ProviderModel();

            await ExecuteWithRetryAsync(async () =>
            {
                provider = await db.Providers.FirstOrDefaultAsync(x => x.ProviderId == request.ProviderId)
                           ?? throw new InvalidOperationException("Provider not found.");
            });

            var oldEmail = provider.EMail;

            var customerService = new CustomerService();
            var options = new CustomerUpdateOptions
            {
                Email = request.NewEMail
            };

            await ExecuteWithRetryAsync(async () =>
            {
                using var trans = await db.Database.BeginTransactionAsync();

                await customerService.UpdateAsync(provider.PayEngineId, options);

                provider.UpdateDate = DateTime.UtcNow;
                provider.EMail = request.NewEMail;
                request.DeleteDate = DateTime.UtcNow;

                await db.SaveChangesAsync();
                await trans.CommitAsync();
            });

            logger.LogAudit(
                $"Email Updated - Provider Id: {provider.ProviderId} - Old E-Mail: {oldEmail} - New E-Mail: {provider.EMail}");
        }

        return response;
    }

    public async Task<BaseDTO> CancelSubscriptionAsync(string jwt)
{
    var response = new BaseDTO();
    var providerId = IdentityTool
        .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

    var utcNow = DateTime.UtcNow;

    await ExecuteWithRetryAsync(async () =>
    {
        using var trans = await db.Database.BeginTransactionAsync();

        var provider = await db.Providers
            .Where(x => x.ProviderId == providerId)
            .Include(x => x.Clients)
            .Include(x => x.Communications)
            .FirstOrDefaultAsync();

        var customerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(provider.TimeZoneCode.ToString().Replace("_", " "));

        var localCancelTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, customerTimeZone);

        // Determine the last day of the local month
        var lastDayLocal = DateTime.DaysInMonth(localCancelTime.Year, localCancelTime.Month);
        var lastDayLocalDate = new DateTime(localCancelTime.Year, localCancelTime.Month, lastDayLocal);

        if (localCancelTime.Date == lastDayLocalDate.Date)
        {
            provider.EndOfService = new DateTime(
                localCancelTime.Year,
                localCancelTime.Month,
                lastDayLocal,
                23, 59, 59,
                DateTimeKind.Utc);
        }
        else
        {
            provider.EndOfService = new DateTime(utcNow.Year, utcNow.Month,
                DateTime.DaysInMonth(utcNow.Year, utcNow.Month), 23, 59, 59, DateTimeKind.Utc);
        }

        foreach (var client in provider.Clients)
        {
            await db.ClientCommunications
                .Where(x => x.Client.ClientId == client.ClientId && provider.EndOfService <= x.SendAfter)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));
        }
        
        await db.ProviderCommunications
                .Where(x => x.ProviderId == providerId && provider.EndOfService <= x.SendAfter)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

        await db.SaveChangesAsync();
        await trans.CommitAsync();
    });

    return response;
}
    
    private bool IsCancellationOnLastDay(TimeZoneCodeEnum customerTimeZoneId)
    {
        var utcCancelTime = DateTime.UtcNow;
        
        var customerTimeZoneName = customerTimeZoneId.ToString().Replace("_", " ");
        var customerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(customerTimeZoneName);
        var localCancelTime = TimeZoneInfo.ConvertTimeFromUtc(utcCancelTime, customerTimeZone);
        var endOfMonth = new DateTime(localCancelTime.Year, localCancelTime.Month, 1)
            .AddMonths(1)
            .AddDays(-1);

        return localCancelTime.Date == endOfMonth.Date;
    }

    // ──────────────────────── Private Methods ────────────────────────
    private async Task ExecuteWithRetryAsync(Func<Task> action, [CallerMemberName] string callerName = "")
    {
        var stopwatch = Stopwatch.StartNew();
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);
        var attempt = 0;

        for (attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    logger.LogError(ex.ToString());
                    throw;
                }

                await Task.Delay(retryDelay);
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInfo(
                    $"{callerName}: {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}