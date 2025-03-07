using System.Net;
using System.Web;

namespace XperienceCommunity.Redirects.Utilities;

public static class UrlSanitizer
{
    private const string ValidQueryKeyPattern = @"^[a-zA-Z0-9._~-]+$";
    private const string ValidAnchorPattern = @"^[a-zA-Z0-9!$&'()*+,;=\-._~:@/?]+$";

    public static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url) || url.Length > Constants.MaxUrlLength)
        {
            return null;
        }   

        return IsAbsoluteUrl(url) 
            ? SanitizeAbsoluteUrl(url) 
            : SanitizeRelativeUrl(url);
    }

    private static bool IsAbsoluteUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string? SanitizeAbsoluteUrl(string url)
    {
        var absoluteUri = new Uri(url);
        
        if (!IsValidScheme(absoluteUri.Scheme))
        {
            return null;
        }

        var builder = new UriBuilder(absoluteUri)
        {
            Path = EncodePath(absoluteUri.AbsolutePath)
        };

        ApplyQueryString(builder, absoluteUri.Query);
        ApplyFragment(builder, absoluteUri.Fragment);

        return builder.Uri.ToString();
    }

    private static bool IsValidScheme(string scheme) =>
        scheme == Uri.UriSchemeHttp || scheme == Uri.UriSchemeHttps;

    private static void ApplyQueryString(UriBuilder builder, string query)
    {
        if (!string.IsNullOrEmpty(query))
        {
            builder.Query = SanitizeQueryParameters(query.TrimStart('?'));
        }
    }

    private static void ApplyFragment(UriBuilder builder, string fragment)
    {
        if (!string.IsNullOrEmpty(fragment))
        {
            builder.Fragment = SanitizeAnchor(fragment.TrimStart('#'));
        }
    }

    private static string? SanitizeRelativeUrl(string url)
    {
        var (urlPart, fragment) = SplitUrlAndFragment(url);
        var (path, query) = SplitPathAndQuery(urlPart);

        var encodedPath = EncodePath(path);
        if (string.IsNullOrEmpty(encodedPath))
        {
            return null;
        }

        var result = encodedPath;
        result = AppendQueryString(result, query);
        result = AppendFragment(result, fragment);

        return result;
    }

    private static (string urlPart, string? fragment) SplitUrlAndFragment(string url)
    {
        var parts = url.Split('#', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static (string path, string? query) SplitPathAndQuery(string urlPart)
    {
        var parts = urlPart.Split('?', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : null);
    }

    private static string AppendQueryString(string url, string? query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return url;
        }

        var sanitizedQuery = SanitizeQueryParameters(query);
        return string.IsNullOrEmpty(sanitizedQuery) 
            ? url 
            : $"{url}?{sanitizedQuery}";
    }

    private static string AppendFragment(string url, string? fragment)
    {
        if (string.IsNullOrEmpty(fragment))
        {
            return url;
        }

        var sanitizedFragment = SanitizeAnchor(fragment);
        return string.IsNullOrEmpty(sanitizedFragment) 
            ? url 
            : $"{url}#{sanitizedFragment}";
    }

    public static string EncodePath(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var parts = url.Split('/');
        return string.Join("/", parts.Select(HttpUtility.UrlEncode));
    }

    public static string SanitizeQueryParameters(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString) || queryString.Length > Constants.MaxQueryStringLength)
        {
            return string.Empty;
        }

        var parameters = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
        
        var encodedParameters = parameters
            .Select(ParseAndEncodeQueryParameter)
            .Where(param => param != null);

        return string.Join("&", encodedParameters);
    }

    private static string? ParseAndEncodeQueryParameter(string param)
    {
        var parts = param.Split('=', 2);
        var key = parts[0].Trim();

        if (!IsValidQueryStringKey(key))
        {
            return null;
        }

        return parts.Length == 1 
            ? key 
            : $"{key}={EncodeQueryValue(parts[1].Trim().Substring(0, Math.Min(parts[1].Length, Constants.MaxQueryValueLength)))}";
    }

    private static string EncodeQueryValue(string value) =>
        HttpUtility.UrlEncode(WebUtility.HtmlEncode(value));

    private static bool IsValidQueryStringKey(string key) =>
        !string.IsNullOrEmpty(key) &&
        key.Length <= Constants.MaxQueryKeyLength &&
        System.Text.RegularExpressions.Regex.IsMatch(key, ValidQueryKeyPattern);

    public static string SanitizeAnchor(string? anchor)
    {
        if (string.IsNullOrEmpty(anchor) || anchor.Length > Constants.MaxAnchorLength)
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.IsMatch(anchor, ValidAnchorPattern)
            ? HttpUtility.UrlEncode(anchor)
            : string.Empty;
    }
} 