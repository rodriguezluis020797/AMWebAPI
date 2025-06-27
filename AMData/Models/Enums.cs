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
    ServerError = 500
}

public enum SessionClaimEnum
{
    Unknown = 0,
    ProviderId,
    SessionId,
    JWToken,
    RefreshToken
}

public enum AccountStatusEnum
{
    Unknown = 0,
    Active,
    ToBeDeactivated,
    Inactive,
    Suspended,
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

    // United States
    US_AL, // Alabama
    US_AK, // Alaska
    US_AZ, // Arizona
    US_AR, // Arkansas
    US_CA, // California
    US_DC, // District of Columbia
    US_CO, // Colorado
    US_CT, // Connecticut
    US_DE, // Delaware
    US_FL, // Florida
    US_GA, // Georgia
    US_HI, // Hawaii
    US_ID, // Idaho
    US_IL, // Illinois
    US_IN, // Indiana
    US_IA, // Iowa
    US_KS, // Kansas
    US_KY, // Kentucky
    US_LA, // Louisiana
    US_ME, // Maine
    US_MD, // Maryland
    US_MA, // Massachusetts
    US_MI, // Michigan
    US_MN, // Minnesota
    US_MS, // Mississippi
    US_MO, // Missouri
    US_MT, // Montana
    US_NE, // Nebraska
    US_NV, // Nevada
    US_NH, // New Hampshire
    US_NJ, // New Jersey
    US_NM, // New Mexico
    US_NY, // New York
    US_NC, // North Carolina
    US_ND, // North Dakota
    US_OH, // Ohio
    US_OK, // Oklahoma
    US_OR, // Oregon
    US_PA, // Pennsylvania
    US_RI, // Rhode Island
    US_SC, // South Carolina
    US_SD, // South Dakota
    US_TN, // Tennessee
    US_TX, // Texas
    US_UT, // Utah
    US_VT, // Vermont
    US_VA, // Virginia
    US_WA, // Washington
    US_WV, // West Virginia
    US_WI, // Wisconsin
    US_WY, // Wyoming

    // Mexico
    MX_AG, // Aguascalientes
    MX_BC, // Baja California
    MX_BS, // Baja California Sur
    MX_CDMX, // Ciudad de México
    MX_CM, // Campeche
    MX_CS, // Chiapas
    MX_CH, // Chihuahua
    MX_CO, // Coahuila
    MX_CL, // Colima
    MX_DF, // Ciudad de México
    MX_DG, // Durango
    MX_GT, // Guanajuato
    MX_GR, // Guerrero
    MX_HG, // Hidalgo
    MX_JA, // Jalisco
    MX_MX, // México (State of Mexico)
    MX_MI, // Michoacán
    MX_MO, // Morelos
    MX_NA, // Nayarit
    MX_NL, // Nuevo León
    MX_OA, // Oaxaca
    MX_PU, // Puebla
    MX_QE, // Querétaro
    MX_QR, // Quintana Roo
    MX_SL, // San Luis Potosí
    MX_SI, // Sinaloa
    MX_SO, // Sonora
    MX_TB, // Tabasco
    MX_TM, // Tamaulipas
    MX_TL, // Tlaxcala
    MX_VE, // Veracruz
    MX_YU, // Yucatán
    MX_ZA // Zacatecas
}

public enum AppointmentStatusEnum
{
    Select = 0,
    Scheduled,
    Completed,
    Cancelled
}