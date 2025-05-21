using System.Globalization;
using System.Text.RegularExpressions;

namespace AMTools.Tools;

public static class ValidationTool
{
    public static void ValidateName(string input, out string output)
    {
        output = input?.Trim() ?? string.Empty;
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                RegexOptions.None, TimeSpan.FromMilliseconds(200));

            string DomainMapper(Match match)
            {
                var idn = new IdnMapping();
                var domainName = idn.GetAscii(match.Groups[2].Value);

                return match.Groups[1].Value + domainName;
            }
        }
        catch (RegexMatchTimeoutException e)
        {
            return false;
        }
        catch (ArgumentException e)
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static void IsValidPhoneNumber(string input, out string output)
    {
        input = input?.Trim() ?? string.Empty;

        // Remove all non-numeric characters
        var digitsOnly = Regex.Replace(input, @"\D", "");

        // Validate length
        if (digitsOnly.Length == 10)
            output = digitsOnly;
        else
            output = string.Empty;
    }

    public static void ValidateZipCode(string input, out string output)
    {
        input = input?.Trim() ?? string.Empty;

        // Remove all non-numeric characters
        var digitsOnly = Regex.Replace(input, @"\D", "");

        // Validate length
        if (digitsOnly.Length == 5)
            output = digitsOnly;
        else
            output = string.Empty;
    }
}