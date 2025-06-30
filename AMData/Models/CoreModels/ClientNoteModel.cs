using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("ClientNote")]
public class ClientNoteModel
{
    public ClientNoteModel(long clientId, string note)
    {
        ClientId = clientId;
        Note = note;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
    }

    [Key] public long ClientNoteId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; }
    public string Note { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ClientModel Client { get; set; }

    public void UpdateRecordFromDTO(ClientNoteDTO dto)
    {
        Note = dto.Note;
        UpdateDate = DateTime.UtcNow;
    }
}