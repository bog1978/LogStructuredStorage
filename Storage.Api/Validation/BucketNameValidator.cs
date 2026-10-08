using System.Text.RegularExpressions;
using Storage.Api.Exceptions;

namespace Storage.Api.Validation;

internal static partial class BucketNameValidator
{
    // Только ASCII-буквы, цифры, '_' и '-'; одинаково безопасно для имён каталогов Windows и Linux.
    [GeneratedRegex(@"\A[A-Za-z0-9_-]{1,16}\z", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedBucketNameRegex();

    [GeneratedRegex(@"\A(?:CON|PRN|AUX|NUL|CONIN\$|CONOUT\$|COM[1-9]|LPT[1-9])\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReservedDeviceNameRegex();

    public static string NormalizeAndValidate(string? bucketName)
    {
        // Сначала проверяем исходное имя: Unicode-символ после ToLowerInvariant() может стать ASCII-буквой.
        if (bucketName is null || !AllowedBucketNameRegex().IsMatch(bucketName))
            throw new InvalidBucketNameException(bucketName);

        var normalizedName = bucketName.ToLowerInvariant();
        if (ReservedDeviceNameRegex().IsMatch(normalizedName))
            throw new InvalidBucketNameException(bucketName);

        return normalizedName;
    }
}
