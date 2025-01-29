using CMS.Base;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.Websites;
using CMS.Websites.Internal;
using Kentico.Xperience.Admin.Base;
using XperienceCommunity.Redirects.UIPages;
using static XperienceCommunity.Redirects.Admin.RedirectConstants;

[assembly: UIPage(
    parentType: typeof(RedirectApplicationPage),
    slug: "list",
    uiPageType: typeof(RedirectListing),
    name: "List",
    templateName: TemplateNames.LISTING,
    order: UIPageOrder.First)]

namespace XperienceCommunity.Redirects.UIPages;

public class RedirectListing : ListingPage
{
    protected override string ObjectType => RedirectInfo.OBJECT_TYPE;

    private readonly IContentQueryExecutor _executor;
    private readonly IWebPageUrlRetriever _webPageUrlRetriever;
    
    public RedirectListing(IContentQueryExecutor executor, IWebPageUrlRetriever webPageUrlRetriever)
    {
        _executor = executor;
        _webPageUrlRetriever = webPageUrlRetriever;
    }

    public override async Task ConfigurePage()
    {
        PageConfiguration.HeaderActions.AddLink<RedirectCreate>("New redirect");
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.AddEditRowAction<RedirectEdit>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(RedirectInfo.RedirectSourceUrl), "Source URL", searchable: true)
            .AddColumn(nameof(RedirectInfo.RedirectTargetWebPageItemGUID), "Target URL", formatter: GetWebPageUrl)
            .AddColumn(nameof(RedirectInfo.RedirectResponseCode), "Redirect code", formatter: GetRedirectCode)
            .AddColumn(nameof(RedirectInfo.RedirectQueryString), "Target web page query string", visible: false)
            .AddColumn(nameof(RedirectInfo.RedirectAnchor), "Target web page anchor", visible: false)
            .AddColumn(nameof(RedirectInfo.RedirectTargetType), "Target type", visible: false)
            .AddColumn(nameof(RedirectInfo.RedirectTargetUrl), "Target URL", visible: false);

        PageConfiguration.AddEditRowAction<RedirectEditSection>();
        
        await base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);
    
    private string GetWebPageUrl(object objectValue, IDataContainer dataContainer)
    {
        dataContainer.TryGetValue(nameof(RedirectInfo.RedirectTargetType), out object? targetTypeObject);
        string targetType = ValidationHelper.GetString(targetTypeObject, "");

        if (targetType == "url")
        {
            dataContainer.TryGetValue(nameof(RedirectInfo.RedirectTargetUrl), out object? targetUrlObject);
            return ValidationHelper.GetString(targetUrlObject, "");
        }

        Guid webPageItemGuid = ValidationHelper.GetGuid(objectValue, Guid.Empty);
        
        if (webPageItemGuid != Guid.Empty)
        {
            var pageQuery = new ContentItemQueryBuilder()
                .ForContentTypes(parameters =>
                    parameters.ForWebsite([webPageItemGuid], includeUrlPath: false)
                )
                .Parameters(parameters => 
                    parameters
                        .Columns(nameof(WebPageItemInfo.WebPageItemID))
                        .TopN(1)
                );

            var result = _executor.GetMappedResult<IWebPageFieldsSource>(pageQuery).Result.FirstOrDefault();

            if (result != null)
            {
                string pageUrl = _webPageUrlRetriever.Retrieve(result.SystemFields.WebPageItemID, GetDefaultLanguageName()).Result.RelativePath;

                if (pageUrl != null)
                {
                    pageUrl = pageUrl.Replace("~", "");

                    string queryString = "";
                    string anchor = "";

                    if (dataContainer.TryGetValue(nameof(RedirectInfo.RedirectQueryString), out object? queryStringObject))
                    {
                        queryString = ValidationHelper.GetString(queryStringObject, "");
                    }

                    if (dataContainer.TryGetValue(nameof(RedirectInfo.RedirectAnchor), out object? anchorObject))
                    {
                        anchor = ValidationHelper.GetString(anchorObject, "");
                    }

                    if (!string.IsNullOrEmpty(queryString))
                    {
                        pageUrl = $"{pageUrl}?{queryString.TrimStart('?')}";
                    }

                    if (!string.IsNullOrEmpty(anchor))
                    {
                        pageUrl = $"{pageUrl}#{anchor.TrimStart('#')}";
                    }

                    return pageUrl;
                }
            }
        }

        return "Page not selected";
    }

    private string GetRedirectCode(object objectValue, IDataContainer dataContainer)
    {        
        string redirectResponseCode = ValidationHelper.GetString(objectValue, RedirectResponseCodeConstants.Permanent);

        if (string.IsNullOrEmpty(redirectResponseCode) || redirectResponseCode == RedirectResponseCodeConstants.Permanent)
        {
            return $"{RedirectResponseCodeConstants.Permanent} – Permanent";
        }

        if (redirectResponseCode == RedirectResponseCodeConstants.Temporary)
        {
            return $"{RedirectResponseCodeConstants.Temporary} – Temporary";
        }

        return redirectResponseCode;
    }

    private string GetDefaultLanguageName()
    {
        return ContentLanguageInfo.Provider.Get()
            .TopN(1)
            .Column(nameof(ContentLanguageInfo.ContentLanguageName))
            .WhereTrue(nameof(ContentLanguageInfo.ContentLanguageIsDefault))
            .GetScalarResult<string>();
    }
}

