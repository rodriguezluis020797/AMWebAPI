﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("ProviderCommunication")]
    public class ProviderCommunicationModel
    {
        [Key] public long CommunicationId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public string Message { get; set; }
        public DateTime SendAfter { get; set; }
        public bool Sent { get; set; }
        public DateTime? AttemptOne { get; set; }
        public DateTime? AttemptTwo { get; set; }
        public DateTime? AttemptThree { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; }
    }
}
