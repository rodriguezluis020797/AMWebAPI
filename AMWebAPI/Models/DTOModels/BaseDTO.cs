namespace AMWebAPI.Models.DTOModels
{
    public class BaseDTO
    {
        public RequestStatusEnum RequestStatus { get; set; } = RequestStatusEnum.Unknown;
        public string ErrorMessage { get; set; } = string.Empty;

        public void ResetModel()
        {
            RequestStatus = RequestStatusEnum.Unknown;
            ErrorMessage = string.Empty;
        }
    }
}
