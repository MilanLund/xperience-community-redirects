using System.Net;
using System.Web;
using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using XperienceCommunity.Redirects.Services;

namespace XperienceCommunity.Redirects;

public class RedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRedirectService _redirectService;
    private readonly IWebPageUrlRetriever _webPageUrlRetriever;
    
    private static readonly string[] ExcludedStartingPaths =
    [
        "/cmsctx",
        "/admin",
        "/getmedia",
        "/getcontentasset",
        "/kentico."
    ];

    public RedirectMiddleware(
        RequestDelegate next,
        IRedirectService redirectService,
        IWebPageUrlRetriever webPageUrlRetriever
        )
    {
        _webPageUrlRetriever = webPageUrlRetriever;
        _next = next;
        _redirectService = redirectService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        
        string requestPath = context.Request.Path.Value?.ToLower() ?? string.Empty;

        foreach (string excludedPath in ExcludedStartingPaths)
        {
            if (context.Request.Path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }
        
        var allRedirects = await _redirectService.GetRedirects();
        if (allRedirects?.Any() != true)
        {
            await _next(context);
            return;
        }
        
        RedirectInfo? matchingRedirectInfo = allRedirects.FirstOrDefault(r => r.RedirectSourceUrl == requestPath);

        if (matchingRedirectInfo != null)
        {
            int? targetWebPageItemId = GetWebPageItemId(matchingRedirectInfo.RedirectTargetWebPageItemGUID);

            if (targetWebPageItemId.HasValue)
            {
                var languages = GetContentLangauges().ToList();

                string firstSegment = context.Request.Path.ToString().Split('/').First();

                if (context.Request.Path.ToString().Split('/').Length > 1)
                {
                    firstSegment = context.Request.Path.ToString().Split('/')[1];
                }

                var currentLanguage = languages.First(l => l.ContentLanguageIsDefault);
                
                if (!string.IsNullOrEmpty(firstSegment))
                {
                    firstSegment = firstSegment?.ToLower() ?? string.Empty;
                    
                    var matchedLanguage = languages.FirstOrDefault(l => 
                        l.ContentLanguageName.Equals(firstSegment, StringComparison.CurrentCultureIgnoreCase)
                        || l.ContentLanguageCultureFormat.Equals(firstSegment, StringComparison.CurrentCultureIgnoreCase));

                    if (matchedLanguage != null)
                    {
                        currentLanguage = matchedLanguage;
                    }
                }
                
                string targetPageUrl = _webPageUrlRetriever.Retrieve(targetWebPageItemId.Value, currentLanguage?.ContentLanguageName).Result.RelativePath.Replace("~", "");

                if (!requestPath.Equals(targetPageUrl, StringComparison.OrdinalIgnoreCase))
                {
                    // Add query string if one is configured
                    if (!string.IsNullOrEmpty(matchingRedirectInfo?.RedirectQueryString))
                    {
                        var sanitizedQueryString = SanitizeQueryParameters(matchingRedirectInfo.RedirectQueryString.Trim().TrimStart('?'));
                        if (!string.IsNullOrEmpty(sanitizedQueryString))
                        {
                            targetPageUrl = $"{targetPageUrl}?{sanitizedQueryString}";
                        }
                    }

                    if (!string.IsNullOrEmpty(matchingRedirectInfo.RedirectAnchor))
                    {
                        var sanitizedAnchor = SanitizeAnchor(matchingRedirectInfo.RedirectAnchor.Trim().TrimStart('#'));
                        if (!string.IsNullOrEmpty(sanitizedAnchor))
                        {
                            targetPageUrl = $"{targetPageUrl}#{sanitizedAnchor}";
                        }
                    }

                    context.Response.Redirect(targetPageUrl, permanent: true);
                
                    await context.Response.CompleteAsync();
                    
                    return;
                }
            }
        }

        await _next(context);
    }

    private int? GetWebPageItemId(Guid webPageItemGuid)
    {
        return WebPageItemInfo.Provider.Get()
            .TopN(1)
            .Column(nameof(WebPageItemInfo.WebPageItemID))
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemGUID), webPageItemGuid)
            .GetScalarResult<int>();
    }
    
    private IEnumerable<ContentLanguageInfo> GetContentLangauges()
    {
        return ContentLanguageInfo.Provider.Get()
            .Columns(nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageCultureFormat),
                nameof(ContentLanguageInfo.ContentLanguageIsDefault))
            .ToList();
    }

    private string SanitizeQueryParameters(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return queryString;
        }

        // Validate query string length
        if (queryString.Length > 2048)
        {
            return string.Empty;
        }

        var parameters = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
        
        var encodedParameters = parameters.Select(ParseAndEncodeQueryParameter)
            .Where(param => param != null);

        return string.Join("&", encodedParameters);
    }

    private string? ParseAndEncodeQueryParameter(string param)
    {
        var parts = param.Split('=', 2); // Split on first '=' only
        var key = parts[0].Trim();

        if (!IsValidQueryStringKey(key))
        {
            return null;
        }

        // If no value part exists, return just the key
        if (parts.Length == 1)
        {
            return key;
        }

        // Encode the value part
        var value = parts[1].Trim();
        var encodedValue = HttpUtility.UrlEncode(
            WebUtility.HtmlEncode(value)
        );
        
        return $"{key}={encodedValue}";
    }

    private bool IsValidQueryStringKey(string key)
    {
        // Allow characters that are valid in query parameter keys:
        // - alphanumeric
        // - !$'()*+,;:@_.-
        // Excluding potential dangerous characters like <>"\{}|^`%#& and spaces
        return !string.IsNullOrEmpty(key) 
               && key.Length <= 200 
               && System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-zA-Z0-9!$'()*+,;:@_.\-]+$");
    }

    private string SanitizeAnchor(string anchor)
    {
        if (string.IsNullOrEmpty(anchor))
        {
            return string.Empty;
        }

        // Validate anchor length (matching query string max length)
        if (anchor.Length > 2048)
        {
            return string.Empty;
        }

        // Allow characters that are valid in URL fragments:
        // - alphanumeric
        // - !$&'()*+,;=-._~:@/?
        // Excluding potential dangerous characters like <>"{}|\^`%# and spaces
        return System.Text.RegularExpressions.Regex.IsMatch(anchor, @"^[a-zA-Z0-9!$&'()*+,;=\-._~:@/?]+$") 
            ? HttpUtility.UrlEncode(anchor) 
            : string.Empty;
    }
}

public static class RedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseXperienceCommunityRedirects(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RedirectMiddleware>();
    }
}
