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

        // Accepts only 10-digit phone numbers (with optional formatting)
        var phonePattern = @"^(?:\(?\d{3}\)?[-.\s]?)?\d{3}[-.\s]?\d{4}$";

        // Remove all non-digit characters before validation
        var digitsOnly = Regex.Replace(input, @"\D", "");

        if (Regex.IsMatch(input, phonePattern) && digitsOnly.Length == 10)
            output = input;
        else
            output = string.Empty;
    }
}