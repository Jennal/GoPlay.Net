namespace GoPlayProj.Utils;

public static class Md5Utils
{
    public static string Encode(string input)
    {
        // Use input string to calculate MD5 hash
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            var inputBytes = System.Text.Encoding.UTF8.GetBytes($"wha_{input}_d2d");
            var hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
    }

    public static string Encode(byte[] input)
    {
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            var hashBytes = md5.ComputeHash(input);
            return Convert.ToHexString(hashBytes);
        }
    }
}