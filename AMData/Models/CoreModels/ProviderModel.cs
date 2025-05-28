using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("Provider")]
public class ProviderModel
{
    public ProviderModel()
    {
    }

    public ProviderModel(long providerId, string firstName, string? middleName, string lastName, string eMail,
        string addressLine1, string addressLine2, string city, string zipCode, CountryCodeEnum countryCode,
        StateCodeEnum stateCode, TimeZoneCodeEnum timeZoneCode, string businessName)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        BusinessName = businessName;
        EMail = eMail;
        EMailVerified = false;
        AccessGranted = false;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
        CountryCode = countryCode;
        StateCode = stateCode;
        TimeZoneCode = timeZoneCode;
        LastLogindate = null;
        TrialEndDate = DateTime.UtcNow.AddMonths(1);
        IsActive = false;
        PayEngineId = null;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        ZipCode = zipCode;
        NextBillingDate = TrialEndDate.AddDays(1);
        SubscriptionToBeCancelled = false;
    }

    [Key] public long ProviderId { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string BusinessName { get; set; }
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
    public string EMail { get; set; }
    public bool EMailVerified { get; set; }
    public CountryCodeEnum CountryCode { get; set; }
    public StateCodeEnum StateCode { get; set; }
    public TimeZoneCodeEnum TimeZoneCode { get; set; }
    public bool AccessGranted { get; set; }
    public DateTime? LastLogindate { get; set; }
    public string? PayEngineId { get; set; }
    public DateTime TrialEndDate { get; set; }
    public bool IsActive { get; set; }
    public bool SubscriptionToBeCancelled { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual List<AppointmentModel> Appointments { get; set; }
    [NotMapped] public virtual List<SessionModel> Sessions { get; set; }
    [NotMapped] public virtual List<ProviderCommunicationModel> Communications { get; set; }
    [NotMapped] public virtual List<ClientModel> Clients { get; set; }
    [NotMapped] public virtual List<UpdateProviderEMailRequestModel> UpdateProviderEMailRequests { get; set; }
    [NotMapped] public virtual VerifyProviderEMailRequestModel VerifyProviderEMailRequest { get; set; }
    [NotMapped] public virtual List<ServiceModel> Services { get; set; }
    [NotMapped] public virtual List<ProviderBillingModel> ProviderBillings { get; set; }
    public List<ResetPasswordRequestModel> ResetPasswordRequests { get; set; }

    public void UpdateRecordFromDTO(ProviderDTO dto)
    {
        FirstName = dto.FirstName;
        MiddleName = dto.MiddleName;
        LastName = dto.LastName;
        UpdateDate = DateTime.UtcNow;
        DeleteDate = null;
        AccessGranted = true;
        CountryCode = dto.CountryCode;
        StateCode = dto.StateCode;
        TimeZoneCode = dto.TimeZoneCode;
        BusinessName = dto.BusinessName;
        AddressLine1 = dto.AddressLine1;
        AddressLine2 = dto.AddressLine2;
        City = dto.City;
        ZipCode = dto.ZipCode;
    }
}