namespace AMData.Models
{
    public enum RequestStatusEnum
    {
        Unknown = 0,
        Success,
        BadRequest,
        Error,
        JWTError
    }
    public enum SessionClaimEnum
    {
        Unknown = 0,
        UserId,
        SessionId,
        JWToken,
        RefreshToken
    }
    public enum SessionActionEnum
    {
        Unknown = 0,
        LogIn,
    }
}
