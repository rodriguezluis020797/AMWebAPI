﻿using AMWebAPI.Models.DTOModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("User")]
    public class UserModel
    {
        [Key] public long UserId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public bool AccessGranted { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual List<SessionModel> Sessions { get; set; }
        [NotMapped] public virtual List<UserCommunicationModel> Communications { get; set; }

        public void CreateNewRecordFromDTO(UserDTO dto)
        {
            FirstName = dto.FirstName;
            MiddleName = dto.MiddleName;
            LastName = dto.LastName;
            EMail = dto.EMail;
            CreateDate = DateTime.UtcNow;
            UpdateDate = null;
            DeleteDate = null;
            AccessGranted = false;
        }
    }
}
