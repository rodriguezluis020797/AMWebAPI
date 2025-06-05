using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ProviderAlert")]
public class ProviderAlertModel
{
    public ProviderAlertModel(){}
    
    public ProviderAlertModel(long providerId, string alert, DateTime alertAfter)
    {
        ProviderId = providerId;
        Alert = alert;
        AlertAfterDate = alertAfter;
        Acknowledged = false;
        CreateDate = DateTime.UtcNow;
    }
    [Key] public long ProviderAlertId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public string Alert { get; set; }
    public DateTime AlertAfterDate {get; set;}
    public bool Acknowledged { get; set; }
    public DateTime CreateDate { get; set; }
    
    [NotMapped] public virtual ProviderModel Provider { get; set; }

    public void UpdateRecordFromDTO()
    {
        
    }
}