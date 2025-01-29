namespace XperienceCommunity.Redirects.Admin;

internal static class RedirectConstants
{
    internal static class ResourceConstants
    {
        public const string ResourceDisplayName = "Redirects";
        public const string ResourceName = "XperienceCommunity.Redirect";
        public const string ResourceDescription = "Allow redirects to be added for web pages";
        public const bool ResourceIsInDevelopment = false;
    }

    internal static class RedirectResponseCodeConstants
    {
        public const string Permanent = "301";
        public const string Temporary = "302";
    }
}