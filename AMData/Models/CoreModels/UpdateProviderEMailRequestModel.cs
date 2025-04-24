using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMData.Models.CoreModels
{
    [Table("UpdateProviderEMailRequest")]
    public class UpdateProviderEMailRequestModel
    {
        [Key] public long UpdateProviderEMailRequestId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public string NewEMail { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; } = new ProviderModel();
    }
}
