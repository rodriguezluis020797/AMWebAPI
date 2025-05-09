﻿using System.ComponentModel.DataAnnotations;
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
        CountryCodeEnum countryCode, StateCodeEnum stateCode, TimeZoneCodeEnum timeZoneCode)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
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
    }

    [Key] public long ProviderId { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string EMail { get; set; }
    public bool EMailVerified { get; set; }
    public CountryCodeEnum CountryCode { get; set; }
    public StateCodeEnum StateCode { get; set; }
    public TimeZoneCodeEnum TimeZoneCode { get; set; }
    public bool AccessGranted { get; set; }
    public DateTime? LastLogindate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual List<AppointmentModel> Appointments { get; set; }
    [NotMapped] public virtual List<SessionModel> Sessions { get; set; }
    [NotMapped] public virtual List<ProviderCommunicationModel> Communications { get; set; }
    [NotMapped] public virtual List<ClientModel> Clients { get; set; }
    [NotMapped] public virtual List<UpdateProviderEMailRequestModel> UpdateProviderEMailRequests { get; set; }
    [NotMapped] public virtual List<ServiceModel> Services { get; set; }

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
    }
}