using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.IdentityModels;

[Table("Password")]
public class PasswordModel
{
    public PasswordModel()
    {
    }

    public PasswordModel(long providerId, bool temporary, string hashedPassword, string salt)
    {
        ProviderId = providerId;
        Temporary = temporary;
        HashedPassword = hashedPassword;
        Salt = salt;
        CreateDate = DateTime.UtcNow;
        DeleteDate = null;
    }

    [Key] public long PasswordId { get; set; }
    public long ProviderId { get; set; }
    public bool Temporary { get; set; }
    public string HashedPassword { get; set; }
    public string Salt { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
}