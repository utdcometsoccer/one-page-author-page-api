using InkStainedWretch.OnePageAuthorAPI.Interfaces;
using System.Text;

namespace InkStainedWretch.OnePageAuthorAPI.Services
{
    public class UsStateCodeService : IUsStateCodeService
    {
        private static readonly IReadOnlyDictionary<string, string> UsStateNameToCode =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["alabama"] = "AL",
                ["alaska"] = "AK",
                ["arizona"] = "AZ",
                ["arkansas"] = "AR",
                ["california"] = "CA",
                ["colorado"] = "CO",
                ["connecticut"] = "CT",
                ["delaware"] = "DE",
                ["florida"] = "FL",
                ["georgia"] = "GA",
                ["hawaii"] = "HI",
                ["idaho"] = "ID",
                ["illinois"] = "IL",
                ["indiana"] = "IN",
                ["iowa"] = "IA",
                ["kansas"] = "KS",
                ["kentucky"] = "KY",
                ["louisiana"] = "LA",
                ["maine"] = "ME",
                ["maryland"] = "MD",
                ["massachusetts"] = "MA",
                ["michigan"] = "MI",
                ["minnesota"] = "MN",
                ["mississippi"] = "MS",
                ["missouri"] = "MO",
                ["montana"] = "MT",
                ["nebraska"] = "NE",
                ["nevada"] = "NV",
                ["new hampshire"] = "NH",
                ["new jersey"] = "NJ",
                ["new mexico"] = "NM",
                ["new york"] = "NY",
                ["north carolina"] = "NC",
                ["north dakota"] = "ND",
                ["ohio"] = "OH",
                ["oklahoma"] = "OK",
                ["oregon"] = "OR",
                ["pennsylvania"] = "PA",
                ["rhode island"] = "RI",
                ["south carolina"] = "SC",
                ["south dakota"] = "SD",
                ["tennessee"] = "TN",
                ["texas"] = "TX",
                ["utah"] = "UT",
                ["vermont"] = "VT",
                ["virginia"] = "VA",
                ["washington"] = "WA",
                ["west virginia"] = "WV",
                ["wisconsin"] = "WI",
                ["wyoming"] = "WY",
                ["district of columbia"] = "DC",
                ["washington dc"] = "DC",
                ["washington d c"] = "DC",
                ["d c"] = "DC",
            };

        public string NormalizeToCode(string? stateOrCode)
        {
            var value = stateOrCode?.Trim();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            // If already a 2-letter code, keep it.
            if (value.Length == 2)
            {
                return value.ToUpperInvariant();
            }

            var key = NormalizeLookupKey(value);
            return UsStateNameToCode.TryGetValue(key, out var code) ? code : value;
        }

        private static string NormalizeLookupKey(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return string.Empty;

            var sb = new StringBuilder(trimmed.Length);
            var previousWasSpace = false;

            foreach (var ch in trimmed)
            {
                if (char.IsLetter(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    previousWasSpace = false;
                    continue;
                }

                if (char.IsWhiteSpace(ch) || ch is '.' or ',' or '-' or '_' or '/')
                {
                    if (!previousWasSpace)
                    {
                        sb.Append(' ');
                        previousWasSpace = true;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
