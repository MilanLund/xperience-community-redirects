using CMS.Websites;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using static XperienceCommunity.Redirects.Admin.RedirectConstants;

namespace XperienceCommunity.Redirects.UIPages;

internal class RedirectEditModel
{
    [RequiredValidationRule]
    [TextInputComponent(
        Label = "Source URL", 
        Order = 1, 
        ExplanationText = "Enter the relative URL to be redirected.", 
        ExplanationTextAsHtml = true)]
    public string? SourceUrl { get; set; }

    [DropDownComponent(
        Label = "Target type",
        Order = 2,
        Options = ";Internal web page\nurl;URL",
        ExplanationText = "Select the target type to which requests for the source URL will be redirected to. Fallbacks to `Internal web page` if not set.",
        ExplanationTextAsHtml = true)]
    public string? RedirectTargetType { get; set; }

    [VisibleIfEqualTo(nameof(RedirectTargetType), "")]
    [WebPageSelectorComponent(
                Label = "Target web page",
                ItemModifierType = typeof(WebPagesWithUrlWebPagePanelItemModifier),
                ExplanationTextAsHtml = true,
                ExplanationText = "Select the target web page to which requests for the source URL will be redirected to.",
                Order = 3,
                MaximumPages = 1)]
    public IEnumerable<WebPageRelatedItem> TargetWebPageItem { get; set; } = Enumerable.Empty<WebPageRelatedItem>();

    [VisibleIfEqualTo(nameof(RedirectTargetType), "")]
    [TextInputComponent(
        Label = "Target web page query string",
        Order = 4,
        ExplanationText = "The query string to be appended to the target web page URL. Example: `?param1=value1&amp;param2=value2`",
        ExplanationTextAsHtml = true)]
    public string? TargetWebPageQueryString { get; set; }

    [VisibleIfEqualTo(nameof(RedirectTargetType), "")]
    [TextInputComponent(
        Label = "Target web page anchor",
        Order = 5,
        ExplanationText = "The anchor to be appended to the target web page URL. Example: `#section`",
        ExplanationTextAsHtml = true)]
    public string? TargetWebPageAnchor { get; set; }

    [VisibleIfEqualTo(nameof(RedirectTargetType), "url")]
    [TextInputComponent(
        Label = "Target URL", 
        Order = 6, 
        ExplanationText = "The URL to which requests for the source URL will be redirected. Could be relative or absolute. Examples: `https://www.example.com/page` or `/sitemap.xml`", 
        ExplanationTextAsHtml = true)]
    public string? TargetUrl { get; set; }

    [DropDownComponent(
        Label = "Response code",
        Order = 7,
        Options = $";{RedirectResponseCodeConstants.Permanent} – Permanent\n{RedirectResponseCodeConstants.Temporary};{RedirectResponseCodeConstants.Temporary} – Temporary",
        ExplanationText = $"Select the redirect code to be used for the redirect. Fallbacks to {RedirectResponseCodeConstants.Permanent} if not set.",
        ExplanationTextAsHtml = true)]
    public string? ResponseCode { get; set; }

    public void MapToRedirectInfo(RedirectInfo info)
    {
        info.RedirectSourceUrl = SourceUrl?.ToLower();
        info.RedirectTargetWebPageItemGUID = TargetWebPageItem.FirstOrDefault()?.WebPageGuid ?? null;
        info.RedirectQueryString = TargetWebPageQueryString;
        info.RedirectAnchor = TargetWebPageAnchor;
        info.RedirectTargetUrl = TargetUrl;
        info.RedirectTargetType = RedirectTargetType;
        info.RedirectResponseCode = ResponseCode;
    }
}