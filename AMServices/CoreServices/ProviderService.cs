﻿using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.DataServices;
using AMServices.PaymentEngineServices;
using MCCDotnetTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.BillingPortal;

namespace AMServices.CoreServices;

public interface IProviderService
{
    Task<ProviderAlertDTO> AcknowledgeProviderAlertAsync(ProviderAlertDTO dto);
    Task<BaseDTO> CancelSubscriptionAsync(string jwt);
    Task<BaseDTO> CreateProviderAsync(ProviderDTO dto);
    Task<ProviderDTO> GetProviderAsync(string jwt, bool generateUrl);
    Task<List<ProviderAlertDTO>> GetProviderAlertsAsync(string jwt);
    Task<ProviderPublicViewDTO> GetProviderPublicViewAsync(string guid);
    Task<List<ProviderReviewDTO>> GetProviderReviewsForProviderAsync(string jwt);
    Task<ProviderReviewDTO> GetProviderReviewForSubmissionAsync(ProviderReviewDTO dto);
    Task<BaseDTO> ReActivateSubscriptionAsync(string jwt);
    Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt);
    Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwt);
    Task<ProviderReviewDTO> UpdateProviderReviewAsync(ProviderReviewDTO dto);
    Task<BaseDTO> VerifyEMailAsync(string guid, bool verifying);
}

public class ProviderService(
    IMCCLogger logger,
    AMCoreData db,
    IConfiguration config,
    IProviderBillingService providerBillingService)
    : IProviderService
{
    public async Task<ProviderAlertDTO> AcknowledgeProviderAlertAsync(ProviderAlertDTO dto)
    {
        var response = new ProviderAlertDTO();
        MCCCryptographyTool.Decrypt(dto.ProviderAlertId, out var decryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await db.ProviderAlerts
                .Where(x => x.ProviderAlertId == long.Parse(decryptedText))
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.Acknowledged, true));

            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<BaseDTO> CancelSubscriptionAsync(string jwt)
    {
        var response = new BaseDTO();
        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        await db.ExecuteWithRetryAsync(async () =>
        {
            var provider = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .FirstOrDefaultAsync();

            if (provider == null) throw new Exception("Provider not found");

            provider.AccountStatus = AccountStatusEnum.ToBeDeactivated;
            provider.UpdateDate = DateTime.UtcNow;

            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<BaseDTO> CreateProviderAsync(ProviderDTO dto)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        await db.ExecuteWithRetryAsync(async () =>
        {
            if (await db.Providers.AnyAsync(x => x.EMail.Equals(dto.EMail)))
                response.ErrorMessage = "Provider with given e-mail already exists.\nPlease wait to be given access.";
        });

        if (!string.IsNullOrEmpty(response.ErrorMessage)) return response;

        var provider = new ProviderModel(dto.FirstName, dto.MiddleName, dto.LastName, dto.EMail,
            dto.AddressLine1, dto.AddressLine2, dto.City, dto.ZipCode, dto.CountryCode, dto.StateCode,
            dto.TimeZoneCode, dto.BusinessName, dto.Description);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            await db.Providers.AddAsync(provider);
            await db.SaveChangesAsync();

            var emailReq = new VerifyProviderEMailRequestModel(provider.ProviderId);
            var message =
                $"Thank you for joining AM Tech!\nPlease verify your email:\n{config["URIs:AngularURI"]}/verify-email?guid={emailReq.QueryGuid}&verifying=true";
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
        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));
        var provider = new ProviderModel();

        var service = new SessionService();
        var url = string.Empty;


        await db.ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers
                           .Where(u => u.ProviderId == providerId)
                           .FirstOrDefaultAsync()
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

            if (provider.DeleteDate != null) provider.DeleteDate = null;

            if (provider.LastLogindate == null)
            {
                var timeZoneStr = provider.TimeZoneCode.ToString().Replace("_", " ");


                provider.TrialEndDate = DateTime.UtcNow.AddMonths(1);
                provider.NextBillingDate = provider.TrialEndDate.AddDays(1);

                var alert = new ProviderAlertModel(provider.ProviderId,
                    $"Your free trial starts now. It will end on {MCCDateTimeTool.ConvertUtcToLocal(provider.TrialEndDate, timeZoneStr): M/d/yyyy}",
                    DateTime.UtcNow);
                await db.ProviderAlerts.AddAsync(alert);

                alert = new ProviderAlertModel(provider.ProviderId,
                    $"Next billing date: {MCCDateTimeTool.ConvertUtcToLocal((DateTime)provider.NextBillingDate, timeZoneStr): M/d/yyyy}",
                    DateTime.UtcNow);
                await db.ProviderAlerts.AddAsync(alert);

                alert = new ProviderAlertModel(provider.ProviderId,
                    "Please add a payment method to continue using services after your trial period is over.\nProfile -> Payment & Invoices",
                    DateTime.UtcNow);
                await db.ProviderAlerts.AddAsync(alert);
            }

            provider.LastLogindate = DateTime.UtcNow;


            await db.SaveChangesAsync();
        });

        var dto = new ProviderDTO();
        dto.CreateNewRecordFromModel(provider, url);
        return dto;
    }

    public async Task<List<ProviderAlertDTO>> GetProviderAlertsAsync(string jwt)
    {
        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));
        var response = new List<ProviderAlertDTO>();
        var alerts = new List<ProviderAlertModel>();

        await db.ExecuteWithRetryAsync(async () =>
        {
            alerts = await db.ProviderAlerts
                .Where(x => x.ProviderId == providerId &&
                            x.Acknowledged == false &&
                            DateTime.UtcNow >= x.AlertAfterDate &&
                            x.DeleteDate == null)
                .ToListAsync();
        });

        foreach (var alert in alerts)
        {
            var dto = new ProviderAlertDTO();
            dto.CreateRecordFromModel(alert);
            MCCCryptographyTool.Encrypt(dto.ProviderAlertId, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
            dto.ProviderAlertId = encryptedText;
            response.Add(dto);
        }

        return response;
    }

    public async Task<ProviderPublicViewDTO> GetProviderPublicViewAsync(string guid)
    {
        var response = new ProviderPublicViewDTO();
        var provider = new ProviderModel();

        await db.ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers
                .Where(p => p.ProviderGuid!.Equals(guid))
                .Select(p => new ProviderModel
                {
                    ProviderId = p.ProviderId,
                    ProviderGuid = p.ProviderGuid,
                    BusinessName = p.BusinessName,
                    TimeZoneCode = p.TimeZoneCode,
                    // Add additional mapped ProviderModel fields here...

                    Reviews = p.Reviews
                        .Where(r => r.Submitted && r.DeleteDate == null)
                        .OrderByDescending(r => r.CreateDate)
                        .Select(r => new ProviderReviewModel
                        {
                            ProviderReviewId = r.ProviderReviewId,
                            GuidQuery = r.GuidQuery,
                            ProviderId = r.ProviderId,
                            ClientId = r.ClientId,
                            ReviewText = r.ReviewText,
                            Rating = r.Rating,
                            Submitted = r.Submitted,
                            CreateDate = r.CreateDate,
                            DeleteDate = r.DeleteDate,

                            // Nested ClientModel (only mapped fields)
                            Client = new ClientModel
                            {
                                ClientId = r.Client.ClientId,
                                FirstName = r.Client.FirstName,
                                LastName = r.Client.LastName,
                                PhoneNumber = r.Client.PhoneNumber
                                // Add more mapped fields from ClientModel if needed
                            }
                        })
                        .ToList()
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (provider == null) response.ErrorMessage = "Provider not found";
        });

        if (!string.IsNullOrEmpty(response.ErrorMessage)) return response;

        foreach (var review in provider.Reviews)
            review.Provider = new ProviderModel
            {
                BusinessName = string.Empty
            };

        if (!string.IsNullOrEmpty(response.ErrorMessage)) return response;

        var timeZoneStr = provider.TimeZoneCode.ToString().Replace("_", " ");

        response.CreateNewRecordFromModels(provider);

        foreach (var review in response.ProviderReviews)
        {
            review.CreateDate = MCCDateTimeTool.ConvertUtcToLocal(review.CreateDate, timeZoneStr);

            MCCCryptographyTool.Encrypt(review.ProviderReviewId, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
            review.ProviderReviewId = encryptedText;
        }

        return response;
    }

    public async Task<List<ProviderReviewDTO>> GetProviderReviewsForProviderAsync(string jwt)
    {
        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var response = new List<ProviderReviewDTO>();
        var reviews = new List<ProviderReviewModel>();
        var providerTimeZone = TimeZoneCodeEnum.Select;

        await db.ExecuteWithRetryAsync(async () =>
        {
            reviews = await db.ProviderReviews
                .Where(x => x.ProviderId == providerId &&
                            x.Submitted == true &&
                            x.DeleteDate == null)
                .Include(x => x.Provider)
                .Include(x => x.Client)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();

            providerTimeZone = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        var timeZoneStr = providerTimeZone.ToString().Replace("_", " ");

        foreach (var review in reviews)
        {
            var dto = new ProviderReviewDTO();
            dto.CreateNewRecordFromModel(review);

            MCCCryptographyTool.Encrypt(dto.ProviderReviewId, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
            dto.ProviderReviewId = encryptedText;

            dto.CreateDate = MCCDateTimeTool.ConvertUtcToLocal(dto.CreateDate, timeZoneStr);
            response.Add(dto);
        }

        return response;
    }


    public async Task<ProviderReviewDTO> GetProviderReviewForSubmissionAsync(ProviderReviewDTO dto)
    {
        var response = new ProviderReviewDTO();
        var review = new ProviderReviewModel();

        await db.ExecuteWithRetryAsync(async () =>
        {
            review = await db.ProviderReviews
                .Where(x => x.GuidQuery.Equals(dto.GuidQuery) &&
                            x.DeleteDate == null)
                .Include(x => x.Provider)
                .Include(x => x.Client)
                .FirstOrDefaultAsync();
        });

        if (review == null)
        {
            response = new ProviderReviewDTO
            {
                GuidQuery = dto.GuidQuery,
                ErrorMessage = "Broken or invalid link."
            };
            return response;
        }

        if (review.Submitted)
        {
            response = new ProviderReviewDTO
            {
                GuidQuery = dto.GuidQuery,
                ErrorMessage = "Review has already been submitted."
            };
            return response;
        }

        response.CreateNewRecordFromModel(review);

        MCCCryptographyTool.Encrypt(response.ProviderReviewId, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);

        response.ProviderReviewId = encryptedText;

        return response;
    }

    public async Task<BaseDTO> ReActivateSubscriptionAsync(string jwt)
    {
        var response = new BaseDTO();

        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var provider = new ProviderModel();
        await db.ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Include(x => x.Clients)
                .Include(x => x.Communications)
                .FirstOrDefaultAsync();
        });

        if (provider == null) throw new Exception("Provider not found");

        var utcNow = DateTime.UtcNow;
        //var customerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(customerTimeZoneName);
        //var customerLocalNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, customerTimeZone);

        if (utcNow < provider.NextBillingDate)
        {
            await db.ExecuteWithRetryAsync(async () =>
            {
                provider.AccountStatus = AccountStatusEnum.Active;
                await db.SaveChangesAsync();
            });
            return response;
        }

        if (!await providerBillingService.IsThereADefaultPaymentMetho(provider.PayEngineId!))
        {
            response.ErrorMessage = "No Default Payment Method Found";
            return response;
        }

        var invoiceItems = new List<InvoiceItemCreateOptions>
        {
            new()
            {
                Customer = provider.PayEngineId,
                Currency = "usd",
                Description = "AM Tech Base Services.",
                Pricing = new InvoiceItemPricingOptions
                {
                    Price = "price_1RTrwgPmRnZS7JiXUMMF3sQZ"
                },
                Quantity = 1
            }
        };

        await db.ExecuteWithRetryAsync(async () =>
        {
            var capturedPayment = await providerBillingService.CapturePayment(provider.PayEngineId!, invoiceItems);

            if (!capturedPayment.Status.Equals("paid"))
            {
                response.ErrorMessage = "Unable to process transaction.\nPlease review your payment method.";
            }
            else
            {
                provider.AccountStatus = AccountStatusEnum.Active;
                provider.UpdateDate = DateTime.UtcNow;
                provider.NextBillingDate =
                    new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                await db.SaveChangesAsync();
            }
        });

        return response;
    }

    public async Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt)
    {
        var response = new ProviderDTO();
        var existingProviderExists = false;
        var existingEMailRequestExists = false;
        await db.ExecuteWithRetryAsync(async () =>
        {
            existingProviderExists = await db.Providers
                .Where(x => x.EMail == dto.EMail)
                .AnyAsync();

            existingEMailRequestExists = await db.UpdateProviderEMailRequests
                .Where(x => x.NewEMail == dto.EMail && x.DeleteDate == null)
                .AnyAsync();
        });
        if (!MCCValidationTool.IsValidEmail(dto.EMail) || existingProviderExists || existingEMailRequestExists)
        {
            response.ErrorMessage = "Provider with given e-mail already exists or e-mail is not in valid format.";
            return response;
        }


        var providerId =
            MCCIdentityTool.GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var request = new UpdateProviderEMailRequestModel(providerId, dto.EMail);
        var message =
            $"There has been a request to change your E-Mail.\n" +
            $"If this was not you, please change your password.\n" +
            $"Otherwise, verify here: {config["URIs:AngularURI"]}/verify-email?guid={request.QueryGuid}&verifying=false\n" +
            $"New E-Mail: {request.NewEMail}";

        var communication = new ProviderCommunicationModel(providerId, message, DateTime.MinValue);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await using var trans = await db.Database.BeginTransactionAsync();

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

        var providerId = MCCIdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var provider = new ProviderModel();

        await db.ExecuteWithRetryAsync(async () =>
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

        await db.ExecuteWithRetryAsync(async () =>
        {
            await customerService.UpdateAsync(provider.PayEngineId, options);
            await db.SaveChangesAsync();
        });
        return response;
    }

    public async Task<ProviderReviewDTO> UpdateProviderReviewAsync(ProviderReviewDTO dto)
    {
        var response = new ProviderReviewDTO();
        var providerReviewModel = new ProviderReviewModel();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        await db.ExecuteWithRetryAsync(async () =>
        {
            providerReviewModel = await db.ProviderReviews
                .Where(x => x.GuidQuery.Equals(dto.GuidQuery) &&
                            x.Submitted == false)
                .Include(x => x.Client)
                .FirstOrDefaultAsync();

            if (providerReviewModel == null)
            {
                response = new ProviderReviewDTO
                {
                    GuidQuery = dto.GuidQuery,
                    ErrorMessage = "Broken or invalid link."
                };
                return;
            }

            providerReviewModel.UpdateRecordFromDto(dto);

            var providerAlert = new ProviderAlertModel(providerReviewModel.ProviderId,
                "You have a new review! Go to your metrics to see it.", DateTime.UtcNow);

            db.ProviderAlerts.Add(providerAlert);

            await db.SaveChangesAsync();
        });
        response.ClientName = !string.IsNullOrEmpty(providerReviewModel.Client.MiddleName)
            ? $"{providerReviewModel.Client.FirstName} {providerReviewModel.Client.MiddleName} {providerReviewModel.Client.LastName}"
            : $"{providerReviewModel.Client.FirstName} {providerReviewModel.Client.LastName}";

        return response;
    }

    public async Task<BaseDTO> VerifyEMailAsync(string guid, bool verifying)
    {
        var response = new BaseDTO();
        if (verifying)
        {
            var request = new VerifyProviderEMailRequestModel();

            await db.ExecuteWithRetryAsync(async () =>
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

            await db.ExecuteWithRetryAsync(async () =>
            {
                provider = await db.Providers
                    .FirstOrDefaultAsync(x => x.ProviderId == request.ProviderId);
            });
            var message =
                $"Thank you for verifying your e-mail!\n" +
                $"You will receive an e-mail when you have been given access to the system.";

            var comm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

            await db.ExecuteWithRetryAsync(async () =>
            {
                await using var trans = await db.Database
                    .BeginTransactionAsync();

                db.ProviderCommunications.Add(comm);


                request.DeleteDate = DateTime.UtcNow;

                provider.UpdateDate = DateTime.UtcNow;
                provider.EMailVerified = true;
                provider.TrialEndDate = DateTime.UtcNow.AddDays(14);
                provider.NextBillingDate = provider.TrialEndDate.AddDays(1);

                provider.PayEngineId = await providerBillingService
                    .CreateProviderBillingProfileAsync(provider.EMail, provider.BusinessName, provider.FirstName,
                        provider.MiddleName, provider.LastName);

                await db.SaveChangesAsync();
                await trans.CommitAsync();
            });
        }
        else
        {
            var request = new UpdateProviderEMailRequestModel();

            await db.ExecuteWithRetryAsync(async () =>
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

            await db.ExecuteWithRetryAsync(async () =>
            {
                provider = await db.Providers.FirstOrDefaultAsync(x => x.ProviderId == request.ProviderId)
                           ?? throw new Exception(nameof(request.ProviderId));
            });

            var oldEmail = provider.EMail;

            await db.ExecuteWithRetryAsync(async () =>
            {
                await using var trans = await db.Database.BeginTransactionAsync();

                provider.UpdateDate = DateTime.UtcNow;
                provider.EMail = request.NewEMail;
                request.DeleteDate = DateTime.UtcNow;

                await providerBillingService.UpdateProviderBillingProfile(provider);

                await db.SaveChangesAsync();
                await trans.CommitAsync();
            });

            logger.LogAudit(
                $"Email Updated - Provider Id: {provider.ProviderId} - Old E-Mail: {oldEmail} - New E-Mail: {provider.EMail}");
        }

        return response;
    }
}