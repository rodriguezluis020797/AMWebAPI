using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("SessionAction")]
public class SessionActionModel
{
    public SessionActionModel()
    {
    }

    public SessionActionModel(SessionActionEnum sessionAction)
    {
        SessionAction = sessionAction;
        CreateDate = DateTime.UtcNow;
    }

    [Key] public long SessionActionId { get; set; }
    [ForeignKey("SessionModel")] public long SessionId { get; set; }
    public SessionActionEnum SessionAction { get; set; }
    public DateTime CreateDate { get; set; }
    [NotMapped] public virtual SessionModel Session { get; set; }
}