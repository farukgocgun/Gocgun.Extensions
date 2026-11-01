using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Text;

namespace Gocgun.Extensions;

/// <summary>Text helpers. Case changes take the caller's culture.</summary>
public static class StringExtensions
{
    /// <summary>True when <paramref name="value"/> has a non-white character.</summary>
    /// <param name="value">The text.</param>
    /// <returns>True when the text is not null or white space.</returns>
    public static bool HasValue([NotNullWhen(true)] this string? value) =>
        !string.IsNullOrWhiteSpace(value);

    /// <summary>Ordinal, case-insensitive equality.</summary>
    /// <param name="source">Left text.</param>
    /// <param name="other">Right text.</param>
    /// <returns>True when the texts match.</returns>
    public static bool EqualsOrdinalIgnoreCase(this string? source, string? other) =>
        string.Equals(source, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the first <paramref name="length"/> characters, or empty.</summary>
    /// <param name="value">The text.</param>
    /// <param name="length">How many characters to keep. The absolute value is used.</param>
    /// <returns>The prefix.</returns>
    public static string Left(this string? value, int length)
    {
        if (!value.HasValue())
        {
            return string.Empty;
        }

        length = Math.Abs(length);
        return value[..Math.Min(value.Length, length)];
    }

    /// <summary>Returns the last <paramref name="length"/> characters, or empty.</summary>
    /// <param name="value">The text.</param>
    /// <param name="length">How many characters to keep. The absolute value is used.</param>
    /// <returns>The suffix.</returns>
    public static string Right(this string? value, int length)
    {
        if (!value.HasValue())
        {
            return string.Empty;
        }

        length = Math.Abs(length);
        return value[^Math.Min(value.Length, length)..];
    }

    /// <summary>True when <paramref name="value"/> is in <paramref name="list"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="list">Candidates.</param>
    /// <param name="comparer">Comparer. Ordinal ignore-case when null.</param>
    /// <returns>True when the list contains the text.</returns>
    public static bool In(this string? value, ICollection<string> list, IEqualityComparer<string>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (!value.HasValue())
        {
            return false;
        }

        comparer ??= StringComparer.OrdinalIgnoreCase;
        return list.Contains(value, comparer);
    }

    /// <summary>Cuts <paramref name="value"/> to <paramref name="maxLength"/> characters.</summary>
    /// <param name="value">The text.</param>
    /// <param name="maxLength">Maximum length. Negative values throw.</param>
    /// <returns>The truncated text, or the original when it is empty.</returns>
    public static string TruncateString(this string value, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        if (value.Length == 0)
        {
            return value;
        }

        return value[..Math.Min(value.Length, maxLength)];
    }

    /// <summary>Parses true or false. Anything else returns <paramref name="defaultValue"/>.</summary>
    /// <param name="source">The text.</param>
    /// <param name="defaultValue">Value used when the text is not a boolean.</param>
    /// <returns>The parsed boolean, or the default.</returns>
    public static bool? ToBoolean(this string? source, bool? defaultValue = null)
    {
        if (!source.HasValue())
        {
            return defaultValue;
        }

        source = source.Trim();
        if (source.EqualsOrdinalIgnoreCase(bool.TrueString))
        {
            return true;
        }

        if (source.EqualsOrdinalIgnoreCase(bool.FalseString))
        {
            return false;
        }

        return defaultValue;
    }

    /// <summary>True when every character is a digit and the length is at least <paramref name="minLength"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="minLength">Minimum length. This is not a maximum.</param>
    /// <returns>True for a long-enough digit string.</returns>
    public static bool IsNumeric(this string? value, int minLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        if (!value.HasValue() || value.Length < minLength)
        {
            return false;
        }

        return value.All(static character => character is >= '0' and <= '9');
    }

    /// <summary>True when <paramref name="email"/> round-trips through <see cref="MailAddress"/>.</summary>
    /// <param name="email">The address.</param>
    /// <returns>True for a simple address.</returns>
    public static bool IsEmail(this string? email)
    {
        if (!email.HasValue())
        {
            return false;
        }

        try
        {
            email = email.Trim();
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Drops non-spacing marks. Does not allocate on the stack.</summary>
    /// <param name="value">The text.</param>
    /// <returns>The text without diacritics.</returns>
    public static string RemoveDiacritics(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var formD = value.Normalize(NormalizationForm.FormD);
        var rented = ArrayPool<char>.Shared.Rent(formD.Length);
        try
        {
            var written = 0;
            foreach (var character in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    rented[written++] = character;
                }
            }

            return new string(rented, 0, written).Normalize(NormalizationForm.FormC);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Builds an ASCII-safe file name. Dotless ı is mapped because it has no decomposition.
    /// Empty input returns a random GUID. Separators and parent-directory segments do not survive.
    /// </summary>
    /// <param name="value">The proposed file name.</param>
    /// <returns>A name safe to place in one directory.</returns>
    public static string CleanFileName(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
        }

        var mapped = string.Create(value.Length, value, static (destination, source) =>
        {
            for (var index = 0; index < source.Length; index++)
            {
                destination[index] = MapFileNameChar(source[index]);
            }
        });
        var stripped = mapped.Replace("..", ".", StringComparison.Ordinal).RemoveDiacritics();
        return string.Create(stripped.Length, stripped, static (destination, source) =>
        {
            for (var index = 0; index < source.Length; index++)
            {
                var character = source[index];
                destination[index] = IsAsciiFileChar(character) ? character : '_';
            }
        });
    }

    /// <summary>Capitalizes each word with <paramref name="culture"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="culture">Culture used for casing.</param>
    /// <returns>The capitalized text, or empty.</returns>
    public static string Capitalize(this string? value, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!value.HasValue())
        {
            return string.Empty;
        }

        var parts = value.Split(' ');
        for (var index = 0; index < parts.Length; index++)
        {
            if (parts[index].Length == 0)
            {
                continue;
            }

            parts[index] = string.Concat(
                parts[index][..1].ToUpper(culture),
                parts[index][1..]);
        }

        return string.Join(' ', parts);
    }

    /// <summary>Title-cases a sentence with <paramref name="culture"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="culture">Culture used for casing.</param>
    /// <returns>The cased sentence, or empty.</returns>
    public static string ToPascalCaseSentence(this string? value, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!value.HasValue())
        {
            return string.Empty;
        }

        value = value.Trim();
        if (value.Length == 1)
        {
            return value.ToUpper(culture);
        }

        if (!value.Contains(' ', StringComparison.Ordinal))
        {
            return string.Concat(char.ToUpper(value[0], culture), value[1..].ToLower(culture));
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < words.Length; index++)
        {
            words[index] = string.Concat(char.ToUpper(words[index][0], culture), words[index][1..].ToLower(culture));
        }

        return string.Join(' ', words);
    }

    /// <summary>Masks a value for logs and UI. Capitalization uses <paramref name="culture"/>.</summary>
    /// <param name="value">The text.</param>
    /// <param name="left">Characters to keep at the start of each word.</param>
    /// <param name="right">Characters to keep at the end of each word.</param>
    /// <param name="capitalize">Whether to capitalize the masked text.</param>
    /// <param name="culture">Culture used when capitalizing.</param>
    /// <returns>The masked text, or empty.</returns>
    public static string Mask(this string? value, int left, int right, bool capitalize, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (!value.HasValue())
        {
            return string.Empty;
        }

        if (right < 0)
        {
            right = 0;
        }

        if (left < 0)
        {
            left = 0;
        }

        var words = value.Split(' ');
        for (var index = 0; index < words.Length; index++)
        {
            words[index] = MaskWord(words[index], left, right);
        }

        var joined = string.Join(' ', words);
        return capitalize ? joined.Capitalize(culture) : joined;
    }

    private static string MaskWord(string value, int left, int right)
    {
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (left > value.Length)
        {
            left = value.Length;
        }

        if (right > value.Length)
        {
            right = value.Length;
        }

        var hidden = value.Length - (left + right);
        if (left == value.Length || right == value.Length || hidden <= 0)
        {
            return string.Concat(value.AsSpan(0, 1), new string('*', value.Length - 1));
        }

        if (left > 0 && right > 0)
        {
            return string.Concat(value.AsSpan(0, left), new string('*', hidden), value.AsSpan(value.Length - right, right));
        }

        if (left > 0)
        {
            return string.Concat(value.AsSpan(0, left), new string('*', hidden));
        }

        if (right > 0)
        {
            return string.Concat(new string('*', hidden), value.AsSpan(value.Length - right, right));
        }

        return string.Empty;
    }

    private static bool IsAsciiFileChar(char value) =>
        value is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-';

    private static char MapFileNameChar(char value) => value switch
    {
        'ı' => 'i',
        'ğ' => 'g',
        'İ' => 'I',
        'Ğ' => 'G',
        'ç' => 'c',
        'Ç' => 'C',
        'ş' => 's',
        'Ş' => 'S',
        'ö' => 'o',
        'Ö' => 'O',
        'ü' => 'u',
        'Ü' => 'U',
        '.' or ' ' or '\'' or '?' or '/' or '\\' or ':' => '_',
        _ => value,
    };
}
