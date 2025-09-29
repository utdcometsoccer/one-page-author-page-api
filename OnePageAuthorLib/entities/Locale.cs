using InkStainedWretch.OnePageAuthorAPI.API;

namespace InkStainedWretch.OnePageAuthorAPI.Entities
{
    /// <summary>
    /// Entity representing a locale, inherits from LocaleResponse and adds an id property.
    /// </summary>
    public class Locale : LocaleResponse
    {
        /// <summary>
        /// Indicates whether this locale is the default.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        public string LanguageName { get; set; }
        public string RegionName { get; set; }
        public string id { get; set; }

        /// <summary>
        /// Constructor that initializes from a LocaleResponse and language/region names.
        /// </summary>
        public Locale(LocaleResponse response, string languageName, string regionName)
            : base(
                response?.Welcome ?? string.Empty,
                response?.AboutMe ?? string.Empty,
                response?.MyBooks ?? string.Empty,
                response?.Loading ?? string.Empty,
                response?.EmailPrompt ?? string.Empty,
                response?.ContactMe ?? string.Empty,
                response?.EmailLinkText ?? string.Empty,
                response?.NoEmail ?? string.Empty,
                response?.SwitchToLight ?? string.Empty,
                response?.SwitchToDark ?? string.Empty,
                response?.Articles ?? string.Empty)
        {
            id = Guid.NewGuid().ToString();
            LanguageName = languageName;
            RegionName = regionName;
        }

        /// <summary>
        /// Default constructor. Initializes id to a new GUID and all base properties to empty.
        /// </summary>
        public Locale() : base()
        {
            id = Guid.NewGuid().ToString();
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            LanguageName = culture.TwoLetterISOLanguageName;
            RegionName = culture.Name.Length > 3 ? culture.Name.Substring(3) : string.Empty;
        }

        /// <summary>
        /// Constructor that initializes all properties.
        /// </summary>
        public Locale(string welcome, string aboutMe, string myBooks, string loading, string emailPrompt, string contactMe, string emailLinkText, string noEmail, string switchToLight, string switchToDark, string articles)
            : base(welcome, aboutMe, myBooks, loading, emailPrompt, contactMe, emailLinkText, noEmail, switchToLight, switchToDark, articles)
        {
            id = Guid.NewGuid().ToString();
            var culture = System.Globalization.CultureInfo.CurrentCulture;
            LanguageName = culture.TwoLetterISOLanguageName;
            RegionName = culture.Name.Length > 3 ? culture.Name.Substring(3) : string.Empty;
        }
    }
}
