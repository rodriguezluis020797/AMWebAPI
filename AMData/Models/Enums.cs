namespace AMData.Models
{
    public enum HttpStatusCodeEnum
    {
        Unknown = 0,
        Success = 200,
        ServerError = 500,
        BadCredentials = 400,
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
