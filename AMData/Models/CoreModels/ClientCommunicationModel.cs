using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ClientCommunication")]
public class ClientCommunicationModel(long clientId, string message, DateTime sendAfter)
{
    [Key] public long ClientCommunicationId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; } = clientId;
    public string Message { get; set; } = message;
    public DateTime SendAfter { get; set; } = sendAfter;
    public bool Sent { get; set; } = false;
    public DateTime? AttemptOne { get; set; } = null;
    public DateTime? AttemptTwo { get; set; } = null;
    public DateTime? AttemptThree { get; set; } = null;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeleteDate { get; set; } = null;
    [NotMapped] public virtual ClientModel Client { get; set; }
}