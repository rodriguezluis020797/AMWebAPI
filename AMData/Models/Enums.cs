namespace AMData.Models;

public enum HttpStatusCodeEnum
{
    Unknown = 0,
    Success = 200,
    LoggedIn = 201,
    NotLoggedIn = 202,
    BadCredentials = 400,
    Unauthorized = 401,
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

public enum CountryCodeEnum
{
    Select,
    United_States,
    Mexico
}

public enum TimeZoneCodeEnum
{
    Select,
    Pacific_Standard_Time, // United States and Mexico
    Mountain_Standard_Time, // United States and Mexico
    Central_Standard_Time, // United States and Mexico
    Eastern_Standard_Time, // United States and Mexico
    Alaska_Standard_Time, // United States
    Hawaii_Standard_Time // United States
}

public enum StateCodeEnum
{
    Select,
    Aguascalientes, // Mexico
    Alabama, // USA
    Alaska, // USA
    Arizona, // USA
    Arkansas, // USA
    Baja_California, // Mexico
    Baja_California_Sur, // Mexico
    Campeche, // Mexico
    California, // USA
    Chiapas, // Mexico
    Chihuahua, // Mexico
    Ciudad_de_México, // Mexico
    Coahuila, // Mexico
    Colima, // Mexico
    Colorado, // USA
    Connecticut, // USA
    Delaware, // USA
    District_of_Columbia, // USA
    Durango, // Mexico
    Florida, // USA
    Georgia, // USA
    Guanajuato, // Mexico
    Guerrero, // Mexico
    Hawaii, // USA
    Hidalgo, // Mexico
    Idaho, // USA
    Illinois, // USA
    Indiana, // USA
    Iowa, // USA
    Jalisco, // Mexico
    Kansas, // USA
    Kentucky, // USA
    Louisiana, // USA
    Maine, // USA
    Maryland, // USA
    Massachusetts, // USA
    Michigan, // USA
    Minnesota, // USA
    Mississippi, // USA
    Missouri, // USA
    Montana, // USA
    México, // Mexico
    Michoacán, // Mexico
    Morelos, // Mexico
    Nayarit, // Mexico
    Nebraska, // USA
    Nevada, // USA
    New_Hampshire, // USA
    New_Jersey, // USA
    New_Mexico, // USA
    New_York, // USA
    North_Carolina, // USA
    North_Dakota, // USA
    Nuevo_León, // Mexico
    Oaxaca, // Mexico
    Ohio, // USA
    Oklahoma, // USA
    Oregon, // USA
    Pennsylvania, // USA
    Puebla, // Mexico
    Querétaro, // Mexico
    Quintana_Roo, // Mexico
    Rhode_Island, // USA
    San_Luis_Potosí, // Mexico
    Sinaloa, // Mexico
    Sonora, // Mexico
    South_Carolina, // USA
    South_Dakota, // USA
    Tabasco, // Mexico
    Tamaulipas, // Mexico
    Tennessee, // USA
    Texas, // USA
    Tlaxcala, // Mexico
    Utah, // USA
    Vermont, // USA
    Veracruz, // Mexico
    Virginia, // USA
    Washington, // USA
    West_Virginia, // USA
    Wisconsin, // USA
    Wyoming, // USA
    Yucatán, // Mexico
    Zacatecas // Mexico
}

public enum AppointmentStatusEnum
{
    Unknown = 0,
    Scheduled,
    Completed,
    Cancelled,
}