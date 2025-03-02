using AMWebAPI.Models.DTOModels.User;

namespace AMWebAPI.Models.CoreModels
{
    public class UserModel
    {
        public long UserId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string EMail { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public DateTime DeleteDate { get; set; }

        public void CreateNewRecordFromDTO(CreateUserDTO dto)
        {
            FirstName = dto.FirstName;
            MiddleName = dto.MiddleName;
            LastName = dto.LastName;
            EMail = dto.EMail;
        }
    }
}
