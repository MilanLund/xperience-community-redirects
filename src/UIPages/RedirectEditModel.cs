using CMS.Websites;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites;
using Kentico.Xperience.Admin.Websites.FormAnnotations;

namespace XperienceCommunity.Redirects.UIPages;

internal class RedirectEditModel
{
    [RequiredValidationRule]
    [TextInputComponent(Label = "Source URL", Order = 1)]
    public string? SourceUrl { get; set; }

    [DropDownComponent(
        Label = "Target type",
        Order = 2,
        Options = ";Internal web page\nexternal;External URL",
        ExplanationText = "Select the target type to which requests for the source URL will be redirected to.",
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

    [VisibleIfEqualTo(nameof(RedirectTargetType), "external")]
    [TextInputComponent(
        Label = "Target external absolute URL", 
        Order = 6, 
        ExplanationText = "The absolute URL to which requests for the source URL will be redirected to.", 
        ExplanationTextAsHtml = true)]
    public string? TargetExternalAbsoluteUrl { get; set; }

    [DropDownComponent(
        Label = "Response code",
        Order = 7,
        Options = ";301 – Permanent\n302;302 – Temporary",
        ExplanationText = "Select the redirect code to be used for the redirect.",
        ExplanationTextAsHtml = true)]
    public string? ResponseCode { get; set; }

    public void MapToRedirectInfo(RedirectInfo info)
    {
        info.RedirectSourceUrl = SourceUrl?.ToLower();
        info.RedirectTargetWebPageItemGUID = TargetWebPageItem.FirstOrDefault()?.WebPageGuid ?? null;
        info.RedirectQueryString = TargetWebPageQueryString;
        info.RedirectAnchor = TargetWebPageAnchor;
        info.RedirectTargetExternalAbsoluteUrl = TargetExternalAbsoluteUrl;
        info.RedirectTargetType = RedirectTargetType;
        info.RedirectResponseCode = ResponseCode;
    }
}