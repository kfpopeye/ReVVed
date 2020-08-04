using WPFLocalizeExtension.Extensions;

namespace RVVD
{
    public static class LocalizationProvider
    {
        public static T GetLocalizedValue<T>(string key)
        {
            return LocExtension.GetLocalizedValue<T>("ReVVed:Language:" + key);
        }
    }
}

