﻿namespace AMData.Models
{
    public enum HttpStatusCodeEnum
    {
        Unknown = 0,
        Success = 200,
        BadCredentials = 400,
        //401 used for unauthorized
        BadPassword = 402,
        ServerError = 500,
        SystemUnavailable = 501
    }
    public enum SessionClaimEnum
    {
        Unknown = 0,
        ProviderId,
        SessionId,
        JWToken,
        RefreshToken
    }
    public enum SessionActionEnum
    {
        Unknown = 0,
        LogIn,
        ChangePassword
    }
}
