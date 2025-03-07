using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Admin.Websites;
using static XperienceCommunity.Redirects.Admin.RedirectConstants;

namespace XperienceCommunity.Redirects.Admin;

internal interface IRedirectModuleInstaller
{
    void Install();
}

internal class RedirectModuleInstaller(IInfoProvider<ResourceInfo> resourceInfoProvider) : IRedirectModuleInstaller
{
    public void Install()
    {
        var resourceInfo = InstallModule();
        InstallModuleClasses(resourceInfo);
    }

    private ResourceInfo InstallModule()
    {
        var resourceInfo = resourceInfoProvider.Get(ResourceConstants.ResourceName)
            ?? new ResourceInfo();

        resourceInfo.ResourceDisplayName = ResourceConstants.ResourceDisplayName;
        resourceInfo.ResourceName = ResourceConstants.ResourceName;
        resourceInfo.ResourceDescription = ResourceConstants.ResourceDescription;
        resourceInfo.ResourceIsInDevelopment = ResourceConstants.ResourceIsInDevelopment;

        if (resourceInfo.HasChanged)
        {
            resourceInfoProvider.Set(resourceInfo);
        }

        return resourceInfo;
    }

    private static void InstallModuleClasses(ResourceInfo resourceInfo) => InstallRedirectClass(resourceInfo);

    private static void InstallRedirectClass(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(RedirectInfo.TYPEINFO.ObjectClassName) ??
                                      DataClassInfo.New(RedirectInfo.OBJECT_TYPE);

        info.ClassName = RedirectInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = RedirectInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Redirects";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(RedirectInfo.RedirectID));

        var formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectSourceUrl),
            Visible = true,
            DataType = FieldDataType.LongText,
            Enabled = true,
            AllowEmpty = false,
            Size=Constants.MaxUrlLength,
            Settings = new()
            {
                { nameof(TextInputProperties.Label), "Source URL" },
                { nameof(TextInputProperties.ExplanationText), "Enter the relative URL to be redirected." },
                { nameof(TextInputProperties.ExplanationTextAsHtml), true }
            }
        };
        formItem.SetComponentName(TextAreaComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectTargetType),
            Visible = true,
            DataType = FieldDataType.Text,
            Enabled = true,
            AllowEmpty = true,
            Settings = new()
            {
                { nameof(DropDownComponent.Properties.Label), "Target type" },
                { nameof(DropDownComponent.Properties.Options), ";Internal web page\nurl;URL" },
                { nameof(DropDownComponent.Properties.ExplanationText), "Select the target type to which requests for the source URL will be redirected to. Fallbacks to `Internal web page` if not set." },
                { nameof(DropDownComponent.Properties.ExplanationTextAsHtml), true }
            }
        };
        formItem.SetComponentName(DropDownComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectTargetWebPageItemGUID),
            Visible = true,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = true,
            Settings = new()
            {
                { nameof(WebPageSelectorComponent.Properties.Label), "Target web page" },
                { nameof(WebPageSelectorComponent.Properties.ExplanationText), "Select the target web page to which requests for the source URL will be redirected to." },
                { nameof(WebPageSelectorComponent.Properties.ExplanationTextAsHtml), true },
                { nameof(WebPageSelectorComponent.Properties.MaximumPages), 1 },
                { nameof(WebPageSelectorComponent.Properties.ItemModifierType), typeof(WebPagesWithUrlWebPagePanelItemModifier) }
            }
        };
        formItem.SetComponentName(WebPageSelectorComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectQueryString),
            DataType = FieldDataType.Text,
            AllowEmpty = true,
            Size=Constants.MaxQueryStringLength,
            Visible = true,
            Enabled = true,
            Settings = new()
            {
                { nameof(TextInputProperties.Label), "Target web page query string" },
                { nameof(TextInputProperties.ExplanationText), "The query string to be appended to the target web page URL." },
                { nameof(TextInputProperties.ExplanationTextAsHtml), true },
            }
        };
        formItem.SetComponentName(TextInputComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectAnchor),
            DataType = FieldDataType.Text,
            AllowEmpty = true,
            Size=Constants.MaxAnchorLength,
            Visible = true,
            Enabled = true,
            Settings = new()
            {
                { nameof(TextInputProperties.Label), "Target web page anchor" },
                { nameof(TextInputProperties.ExplanationText), "The anchor to be appended to the target web page URL." },
                { nameof(TextInputProperties.ExplanationTextAsHtml), true },
            }
        };
        formItem.SetComponentName(TextInputComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectTargetUrl),
            DataType = FieldDataType.Text,
            AllowEmpty = true,
            Size=Constants.MaxUrlLength,
            Visible = true,
            Enabled = true,
            Settings = new()
            {
                { nameof(TextInputProperties.Label), "Target URL" },
                { nameof(TextInputProperties.ExplanationText), "The URL to which requests for the source URL will be redirected. Could be relative or absolute. Examples: `https://www.example.com/page` or `/sitemap.xml`" },
                { nameof(TextInputProperties.ExplanationTextAsHtml), true },
            }
        };
        formItem.SetComponentName(TextInputComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);

        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectResponseCode),
            Visible = true,
            DataType = FieldDataType.Text,
            Enabled = true,
            AllowEmpty = true,
            Settings = new()
            {
                { nameof(DropDownComponent.Properties.Label), "Redirect code" },
                { nameof(DropDownComponent.Properties.Options), $";{RedirectResponseCodeConstants.Permanent} – Permanent\n{RedirectResponseCodeConstants.Temporary};{RedirectResponseCodeConstants.Temporary} – Temporary" },
                { nameof(DropDownComponent.Properties.ExplanationText), $"Select the redirect code to be used for the redirect. Fallbacks to {RedirectResponseCodeConstants.Permanent} if not set." },
                { nameof(DropDownComponent.Properties.ExplanationTextAsHtml), true }
            }
        };
        formItem.SetComponentName(DropDownComponent.IDENTIFIER);
        formInfo.AddFormItem(formItem);
        
        formItem = new FormFieldInfo
        {
            Name = nameof(RedirectInfo.RedirectGUID),
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true,
            AllowEmpty = false,
        };
        formInfo.AddFormItem(formItem);

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }

        PopulateRedirectGUIDs();
    }

    /// <summary>
    /// Ensure that the form is not upserted with any existing form
    /// </summary>
    /// <param name="info"></param>
    /// <param name="form"></param>
    private static void SetFormDefinition(DataClassInfo info, FormInfo form)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(form, new());
            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = form.GetXmlDefinition();
        }
    }
    
    /// <summary>
    /// Retrospectively populates the RedirectGUID field for records created using version < v1.0.3 of this package
    /// </summary>
    private static void PopulateRedirectGUIDs()
    {
        List<RedirectInfo> redirectsWithEmptyGuids = RedirectInfo.Provider.Get().WhereEquals(nameof(RedirectInfo.RedirectGUID), Guid.Empty).ToList();

        if (redirectsWithEmptyGuids.Any())
        {
            foreach (RedirectInfo redirect in redirectsWithEmptyGuids)
            {
                redirect.RedirectGUID = new Guid();
                redirect.Update();
            }
        }
    }
}