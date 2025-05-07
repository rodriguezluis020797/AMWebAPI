using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ClientCommunication")]
public class ClientCommunicationModel
{
    [Key] public long ClientCommunicationId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; }
    public string Message { get; set; }
    public DateTime SendAfter { get; set; }
    public bool Sent { get; set; }
    public DateTime? AttemptOne { get; set; }
    public DateTime? AttemptTwo { get; set; }
    public DateTime? AttemptThree { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ClientModel Client { get; set; }
}