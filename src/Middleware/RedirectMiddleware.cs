using CMS.ContentEngine;
using CMS.Websites;
using CMS.Websites.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using XperienceCommunity.Redirects.Services;
using static XperienceCommunity.Redirects.Admin.RedirectConstants;
using XperienceCommunity.Redirects.Utilities;

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
        IWebPageUrlRetriever webPageUrlRetriever)
    {
        _next = next;
        _redirectService = redirectService;
        _webPageUrlRetriever = webPageUrlRetriever;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsHtmlRequest(context) || IsExcludedPath(context))
        {
            await _next(context);
            return;
        }

        var requestPath = GetNormalizedRequestPath(context);
        var redirects = await GetRedirectsAsync();
        var matchingRedirect = redirects?.FirstOrDefault(r => r.RedirectSourceUrl == requestPath);

        if (matchingRedirect == null)
        {
            await _next(context);
            return;
        }

        await HandleRedirect(context, matchingRedirect);
    }

    private static bool IsHtmlRequest(HttpContext context) =>
        context.Request.Headers.Accept.ToString()
            .Contains("text/html", StringComparison.OrdinalIgnoreCase);

    private bool IsExcludedPath(HttpContext context) =>
        ExcludedStartingPaths.Any(excludedPath => 
            context.Request.Path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));

    private async Task<IEnumerable<RedirectInfo>?> GetRedirectsAsync()
    {
        return await _redirectService.GetRedirects();
    }

    private static string GetNormalizedRequestPath(HttpContext context) =>
        context.Request.Path.Value?.ToLower() ?? string.Empty;

    private async Task HandleRedirect(HttpContext context, RedirectInfo redirect)
    {
        var permanent = IsPermanentRedirect(redirect.RedirectResponseCode);

        if (redirect.RedirectTargetType == "url")
        {
            await HandleUrlRedirect(context, redirect, permanent);
            return;
        }

        if (redirect.RedirectTargetWebPageItemGUID != null)
        {
            await HandlePageRedirect(context, redirect, permanent);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleUrlRedirect(HttpContext context, RedirectInfo redirect, bool permanent)
    {
        var redirectTargetUrl = redirect.RedirectTargetUrl?.Trim();
        if (string.IsNullOrEmpty(redirectTargetUrl))
        {
            await _next(context);
            return;
        }

        var sanitizedUrl = UrlSanitizer.SanitizeUrl(redirectTargetUrl);
        if (string.IsNullOrEmpty(sanitizedUrl))
        {
            await _next(context);
            return;
        }

        await PerformRedirect(context, sanitizedUrl, permanent);
    }

    private async Task HandlePageRedirect(HttpContext context, RedirectInfo redirect, bool permanent)
    {
        var targetWebPageItemId = GetWebPageItemId(redirect.RedirectTargetWebPageItemGUID);
        if (!targetWebPageItemId.HasValue)
        {
            await _next(context);
            return;
        }

        var currentLanguage = GetCurrentLanguage(context);
        var targetPageUrl = await BuildTargetPageUrl(targetWebPageItemId.Value, currentLanguage, redirect);
        
        var requestPath = GetNormalizedRequestPath(context);
        if (requestPath.Equals(targetPageUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        await PerformRedirect(context, targetPageUrl, permanent);
    }

    private async Task<string> BuildTargetPageUrl(int targetWebPageItemId, ContentLanguageInfo currentLanguage, RedirectInfo redirect)
    {
        var baseUrl = await GetBasePageUrl(targetWebPageItemId, currentLanguage);
        var urlWithQuery = AppendQueryString(baseUrl, redirect.RedirectQueryString);
        return AppendAnchor(urlWithQuery, redirect.RedirectAnchor);
    }

    private async Task<string> GetBasePageUrl(int targetWebPageItemId, ContentLanguageInfo currentLanguage)
    {
        var pageUrl = await _webPageUrlRetriever.Retrieve(targetWebPageItemId, currentLanguage?.ContentLanguageName);
        return UrlSanitizer.EncodePath(pageUrl.RelativePath.Replace("~", ""));
    }

    private static string AppendQueryString(string url, string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return url;
        }

        var sanitizedQuery = UrlSanitizer.SanitizeQueryParameters(queryString.Trim().TrimStart('?'));
        return string.IsNullOrEmpty(sanitizedQuery) ? url : $"{url}?{sanitizedQuery}";
    }

    private static string AppendAnchor(string url, string? anchor)
    {
        if (string.IsNullOrEmpty(anchor))
        {
            return url;
        }

        var sanitizedAnchor = UrlSanitizer.SanitizeAnchor(anchor.Trim().TrimStart('#'));
        return string.IsNullOrEmpty(sanitizedAnchor) ? url : $"{url}#{sanitizedAnchor}";
    }

    private ContentLanguageInfo GetCurrentLanguage(HttpContext context)
    {
        var languages = GetContentLanguages().ToList();
        var pathSegments = context.Request.Path.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (pathSegments.Length == 0)
        {
            return languages.First(l => l.ContentLanguageIsDefault);
        }

        var firstSegment = pathSegments[0].ToLower();
        return FindLanguageBySegment(languages, firstSegment) 
               ?? languages.First(l => l.ContentLanguageIsDefault);
    }

    private static ContentLanguageInfo? FindLanguageBySegment(IEnumerable<ContentLanguageInfo> languages, string segment) =>
        languages.FirstOrDefault(l =>
            l.ContentLanguageName.Equals(segment, StringComparison.OrdinalIgnoreCase) ||
            l.ContentLanguageCultureFormat.Equals(segment, StringComparison.OrdinalIgnoreCase));

    private static async Task PerformRedirect(HttpContext context, string url, bool permanent)
    {
        context.Response.Redirect(url, permanent);
        await context.Response.CompleteAsync();
    }

    private int? GetWebPageItemId(Guid? webPageItemGuid)
    {
        if (webPageItemGuid == null)
        {
            return null;
        }

        return WebPageItemInfo.Provider.Get()
            .TopN(1)
            .Column(nameof(WebPageItemInfo.WebPageItemID))
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemGUID), webPageItemGuid.Value)
            .GetScalarResult<int>();
    }
    
    private static IEnumerable<ContentLanguageInfo> GetContentLanguages() =>
        ContentLanguageInfo.Provider.Get()
            .Columns(
                nameof(ContentLanguageInfo.ContentLanguageName),
                nameof(ContentLanguageInfo.ContentLanguageCultureFormat),
                nameof(ContentLanguageInfo.ContentLanguageIsDefault))
            .ToList();

    private static bool IsPermanentRedirect(string? responseCode) =>
        string.IsNullOrEmpty(responseCode)
            ? true  // Default to permanent
            : responseCode == RedirectResponseCodeConstants.Permanent;
}

public static class RedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseXperienceCommunityRedirects(this IApplicationBuilder builder) =>
        builder.UseMiddleware<RedirectMiddleware>();
}
