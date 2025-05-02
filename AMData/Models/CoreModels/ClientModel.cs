using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("Client")]
public class ClientModel
{
    public ClientModel(long providerId, string firstName, string? middleName, string lastName, string phoneNumber)
    {
        ProviderId = providerId;
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
    }

    public long ClientId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ProviderModel Provider { get; set; }

    public void UpdateRecordFromDTO(ClientDTO dto)
    {
        FirstName = dto.FirstName;
        MiddleName = dto.MiddleName;
        LastName = dto.LastName;
        PhoneNumber = dto.PhoneNumber;
        UpdateDate = DateTime.UtcNow;
    }
}