using System.Security.Cryptography;
using System.Text;

namespace SheetMusic.Api.Utilities;

public class KeyGenerator
{
    public static string GetUniqueKey(int size)
    {
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        var data = RandomNumberGenerator.GetBytes(size);

        var result = new StringBuilder(size);

        foreach (byte b in data)
        {
            result.Append(chars[b % (chars.Length)]);
        }

        return result.ToString();
    }
}
