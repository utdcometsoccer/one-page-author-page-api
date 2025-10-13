using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InkStainedWretch.OnePageAuthorAPI;
using InkStainedWretch.OnePageAuthorAPI.Entities;
using InkStainedWretch.OnePageAuthorAPI.Interfaces;

namespace OnePageAuthorAPI.DataSeeder
{
    /// <summary>
    /// Console application for seeding StateProvince data into Azure Cosmos DB.
    /// Seeds US, Canada, and Mexico data in English, French, and Spanish.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            try
            {
                var seeder = host.Services.GetRequiredService<StateProvinceSeeder>();
                await seeder.SeedDataAsync();
                Console.WriteLine("Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error occurred during data seeding");
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Add user secrets
                    config.AddUserSecrets<Program>();

                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Get configuration from user secrets, environment variables, or appsettings
                    var cosmosEndpoint = configuration["CosmosDb:Endpoint"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT")
                        ?? "https://localhost:8081"; // Default to Cosmos DB Emulator
                    var cosmosKey = configuration["CosmosDb:Key"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_KEY")
                        ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="; // Emulator key
                    var databaseId = configuration["CosmosDb:Database"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE")
                        ?? "OnePageAuthorDB";

                    // Register Azure Cosmos DB services
                    services.AddCosmosClient(cosmosEndpoint, cosmosKey);
                    services.AddCosmosDatabase(databaseId);

                    // Register StateProvince services
                    services.AddStateProvinceRepository();
                    services.AddStateProvinceServices();

                    // Register data seeder
                    services.AddScoped<StateProvinceSeeder>();

                    // Configure logging
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                });
    }

    /// <summary>
    /// Service responsible for seeding StateProvince data into Cosmos DB.
    /// </summary>
    public class StateProvinceSeeder
    {
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ILogger<StateProvinceSeeder> _logger;

        public StateProvinceSeeder(IStateProvinceService stateProvinceService, ILogger<StateProvinceSeeder> logger)
        {
            _stateProvinceService = stateProvinceService ?? throw new ArgumentNullException(nameof(stateProvinceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Seeds comprehensive StateProvince data for US, Canada, and Mexico in multiple languages.
        /// </summary>
        public async Task SeedDataAsync()
        {
            _logger.LogInformation("Starting StateProvince data seeding...");

            var allData = new List<StateProvince>();

            // Add US states in English, French, and Spanish
            allData.AddRange(GetUSStates());

            // Add Canadian provinces in English and French
            allData.AddRange(GetCanadianProvinces());

            // Add Mexican states in Spanish, English, and French
            allData.AddRange(GetMexicanStates());

            // Add Chinese provinces in Simplified Chinese and English
            allData.AddRange(GetChineseProvinces());

            // Add Taiwan counties/cities in Traditional Chinese and English
            allData.AddRange(GetTaiwanRegions());

            // Add Egyptian governorates in Arabic and English
            allData.AddRange(GetEgyptianGovernorates());

            // Delete all existing StateProvince entries first
            _logger.LogInformation("Deleting all existing StateProvince entries...");
            var deletedCount = await _stateProvinceService.DeleteAllStateProvincesAsync();
            _logger.LogInformation("Deleted {DeletedCount} existing StateProvince entries", deletedCount);

            _logger.LogInformation("Seeding {Count} StateProvince entries...", allData.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var stateProvince in allData)
            {
                try
                {
                    // Skip entries with missing required data
                    if (string.IsNullOrWhiteSpace(stateProvince.Country) || 
                        string.IsNullOrWhiteSpace(stateProvince.Culture) || 
                        string.IsNullOrWhiteSpace(stateProvince.Code))
                    {
                        _logger.LogWarning("Skipping entry with missing data: Country={Country}, Culture={Culture}, Code={Code}", 
                            stateProvince.Country, stateProvince.Culture, stateProvince.Code);
                        continue;
                    }

                    // Create unique ID for each culture variant
                    stateProvince.id = $"{stateProvince.Code}_{stateProvince.Culture}";

                    await _stateProvinceService.CreateStateProvinceAsync(stateProvince);
                    successCount++;
                    _logger.LogDebug("Created: {Code} - {Name} ({Culture})", stateProvince.Code, stateProvince.Name, stateProvince.Culture);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Failed to create StateProvince: {Code} - {Name} ({Culture})",
                        stateProvince.Code, stateProvince.Name, stateProvince.Culture);
                }
            }

            _logger.LogInformation("Data seeding completed. Success: {SuccessCount}, Errors: {ErrorCount}", successCount, errorCount);
        }

        /// <summary>
        /// Gets US states and territories in English, French, and Spanish.
        /// </summary>
        private List<StateProvince> GetUSStates()
        {
            var states = new List<StateProvince>();

            // US States - English
            var usStatesEnglish = new Dictionary<string, string>
            {
                {"AL", "Alabama"}, {"AK", "Alaska"}, {"AZ", "Arizona"}, {"AR", "Arkansas"},
                {"CA", "California"}, {"CO", "Colorado"}, {"CT", "Connecticut"}, {"DE", "Delaware"},
                {"FL", "Florida"}, {"GA", "Georgia"}, {"HI", "Hawaii"}, {"ID", "Idaho"},
                {"IL", "Illinois"}, {"IN", "Indiana"}, {"IA", "Iowa"}, {"KS", "Kansas"},
                {"KY", "Kentucky"}, {"LA", "Louisiana"}, {"ME", "Maine"}, {"MD", "Maryland"},
                {"MA", "Massachusetts"}, {"MI", "Michigan"}, {"MN", "Minnesota"}, {"MS", "Mississippi"},
                {"MO", "Missouri"}, {"MT", "Montana"}, {"NE", "Nebraska"}, {"NV", "Nevada"},
                {"NH", "New Hampshire"}, {"NJ", "New Jersey"}, {"NM", "New Mexico"}, {"NY", "New York"},
                {"NC", "North Carolina"}, {"ND", "North Dakota"}, {"OH", "Ohio"}, {"OK", "Oklahoma"},
                {"OR", "Oregon"}, {"PA", "Pennsylvania"}, {"RI", "Rhode Island"}, {"SC", "South Carolina"},
                {"SD", "South Dakota"}, {"TN", "Tennessee"}, {"TX", "Texas"}, {"UT", "Utah"},
                {"VT", "Vermont"}, {"VA", "Virginia"}, {"WA", "Washington"}, {"WV", "West Virginia"},
                {"WI", "Wisconsin"}, {"WY", "Wyoming"}, {"DC", "District of Columbia"},
                {"AS", "American Samoa"}, {"GU", "Guam"}, {"MP", "Northern Mariana Islands"},
                {"PR", "Puerto Rico"}, {"VI", "Virgin Islands"}
            };

            // US States - French
            var usStatesFrench = new Dictionary<string, string>
            {
                {"AL", "Alabama"}, {"AK", "Alaska"}, {"AZ", "Arizona"}, {"AR", "Arkansas"},
                {"CA", "Californie"}, {"CO", "Colorado"}, {"CT", "Connecticut"}, {"DE", "Delaware"},
                {"FL", "Floride"}, {"GA", "Géorgie"}, {"HI", "Hawaï"}, {"ID", "Idaho"},
                {"IL", "Illinois"}, {"IN", "Indiana"}, {"IA", "Iowa"}, {"KS", "Kansas"},
                {"KY", "Kentucky"}, {"LA", "Louisiane"}, {"ME", "Maine"}, {"MD", "Maryland"},
                {"MA", "Massachusetts"}, {"MI", "Michigan"}, {"MN", "Minnesota"}, {"MS", "Mississippi"},
                {"MO", "Missouri"}, {"MT", "Montana"}, {"NE", "Nebraska"}, {"NV", "Nevada"},
                {"NH", "New Hampshire"}, {"NJ", "New Jersey"}, {"NM", "Nouveau-Mexique"}, {"NY", "New York"},
                {"NC", "Caroline du Nord"}, {"ND", "Dakota du Nord"}, {"OH", "Ohio"}, {"OK", "Oklahoma"},
                {"OR", "Oregon"}, {"PA", "Pennsylvanie"}, {"RI", "Rhode Island"}, {"SC", "Caroline du Sud"},
                {"SD", "Dakota du Sud"}, {"TN", "Tennessee"}, {"TX", "Texas"}, {"UT", "Utah"},
                {"VT", "Vermont"}, {"VA", "Virginie"}, {"WA", "Washington"}, {"WV", "Virginie-Occidentale"},
                {"WI", "Wisconsin"}, {"WY", "Wyoming"}, {"DC", "District de Columbia"},
                {"AS", "Samoa américaines"}, {"GU", "Guam"}, {"MP", "Îles Mariannes du Nord"},
                {"PR", "Porto Rico"}, {"VI", "Îles Vierges"}
            };

            // US States - Spanish
            var usStatesSpanish = new Dictionary<string, string>
            {
                {"AL", "Alabama"}, {"AK", "Alaska"}, {"AZ", "Arizona"}, {"AR", "Arkansas"},
                {"CA", "California"}, {"CO", "Colorado"}, {"CT", "Connecticut"}, {"DE", "Delaware"},
                {"FL", "Florida"}, {"GA", "Georgia"}, {"HI", "Hawái"}, {"ID", "Idaho"},
                {"IL", "Illinois"}, {"IN", "Indiana"}, {"IA", "Iowa"}, {"KS", "Kansas"},
                {"KY", "Kentucky"}, {"LA", "Luisiana"}, {"ME", "Maine"}, {"MD", "Maryland"},
                {"MA", "Massachusetts"}, {"MI", "Michigan"}, {"MN", "Minnesota"}, {"MS", "Mississippi"},
                {"MO", "Missouri"}, {"MT", "Montana"}, {"NE", "Nebraska"}, {"NV", "Nevada"},
                {"NH", "New Hampshire"}, {"NJ", "Nueva Jersey"}, {"NM", "Nuevo México"}, {"NY", "Nueva York"},
                {"NC", "Carolina del Norte"}, {"ND", "Dakota del Norte"}, {"OH", "Ohio"}, {"OK", "Oklahoma"},
                {"OR", "Oregón"}, {"PA", "Pensilvania"}, {"RI", "Rhode Island"}, {"SC", "Carolina del Sur"},
                {"SD", "Dakota del Sur"}, {"TN", "Tennessee"}, {"TX", "Texas"}, {"UT", "Utah"},
                {"VT", "Vermont"}, {"VA", "Virginia"}, {"WA", "Washington"}, {"WV", "Virginia Occidental"},
                {"WI", "Wisconsin"}, {"WY", "Wyoming"}, {"DC", "Distrito de Columbia"},
                {"AS", "Samoa Americana"}, {"GU", "Guam"}, {"MP", "Islas Marianas del Norte"},
                {"PR", "Puerto Rico"}, {"VI", "Islas Vírgenes"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in usStatesEnglish)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "en-US" });
            }

            foreach (var kvp in usStatesFrench)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "fr-US" });
            }

            foreach (var kvp in usStatesSpanish)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "es-US" });
            }

            return states;
        }

        /// <summary>
        /// Gets Canadian provinces and territories in English and French.
        /// </summary>
        private List<StateProvince> GetCanadianProvinces()
        {
            var provinces = new List<StateProvince>();

            // Canadian Provinces - English
            var canadianProvincesEnglish = new Dictionary<string, string>
            {
                {"AB", "Alberta"}, {"BC", "British Columbia"}, {"MB", "Manitoba"},
                {"NB", "New Brunswick"}, {"NL", "Newfoundland and Labrador"}, {"NS", "Nova Scotia"},
                {"ON", "Ontario"}, {"PE", "Prince Edward Island"}, {"QC", "Quebec"},
                {"SK", "Saskatchewan"}, {"NT", "Northwest Territories"}, {"NU", "Nunavut"},
                {"YT", "Yukon"}
            };

            // Canadian Provinces - French
            var canadianProvincesFrench = new Dictionary<string, string>
            {
                {"AB", "Alberta"}, {"BC", "Colombie-Britannique"}, {"MB", "Manitoba"},
                {"NB", "Nouveau-Brunswick"}, {"NL", "Terre-Neuve-et-Labrador"}, {"NS", "Nouvelle-Écosse"},
                {"ON", "Ontario"}, {"PE", "Île-du-Prince-Édouard"}, {"QC", "Québec"},
                {"SK", "Saskatchewan"}, {"NT", "Territoires du Nord-Ouest"}, {"NU", "Nunavut"},
                {"YT", "Yukon"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in canadianProvincesEnglish)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "en-CA" });
            }

            foreach (var kvp in canadianProvincesFrench)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "fr-CA" });
            }

            return provinces;
        }

        /// <summary>
        /// Gets Mexican states in Spanish, English, and French.
        /// </summary>
        private List<StateProvince> GetMexicanStates()
        {
            var states = new List<StateProvince>();

            // Mexican States - Spanish
            var mexicanStatesSpanish = new Dictionary<string, string>
            {
                {"AGU", "Aguascalientes"}, {"BCN", "Baja California"}, {"BCS", "Baja California Sur"},
                {"CAM", "Campeche"}, {"CHP", "Chiapas"}, {"CHH", "Chihuahua"}, {"CMX", "Ciudad de México"},
                {"COA", "Coahuila"}, {"COL", "Colima"}, {"DUR", "Durango"}, {"GUA", "Guanajuato"},
                {"GRO", "Guerrero"}, {"HID", "Hidalgo"}, {"JAL", "Jalisco"}, {"MEX", "México"},
                {"MIC", "Michoacán"}, {"MOR", "Morelos"}, {"NAY", "Nayarit"}, {"NLE", "Nuevo León"},
                {"OAX", "Oaxaca"}, {"PUE", "Puebla"}, {"QUE", "Querétaro"}, {"ROO", "Quintana Roo"},
                {"SLP", "San Luis Potosí"}, {"SIN", "Sinaloa"}, {"SON", "Sonora"}, {"TAB", "Tabasco"},
                {"TAM", "Tamaulipas"}, {"TLA", "Tlaxcala"}, {"VER", "Veracruz"}, {"YUC", "Yucatán"},
                {"ZAC", "Zacatecas"}
            };

            // Mexican States - English
            var mexicanStatesEnglish = new Dictionary<string, string>
            {
                {"AGU", "Aguascalientes"}, {"BCN", "Baja California"}, {"BCS", "Baja California Sur"},
                {"CAM", "Campeche"}, {"CHP", "Chiapas"}, {"CHH", "Chihuahua"}, {"CMX", "Mexico City"},
                {"COA", "Coahuila"}, {"COL", "Colima"}, {"DUR", "Durango"}, {"GUA", "Guanajuato"},
                {"GRO", "Guerrero"}, {"HID", "Hidalgo"}, {"JAL", "Jalisco"}, {"MEX", "State of Mexico"},
                {"MIC", "Michoacán"}, {"MOR", "Morelos"}, {"NAY", "Nayarit"}, {"NLE", "Nuevo León"},
                {"OAX", "Oaxaca"}, {"PUE", "Puebla"}, {"QUE", "Querétaro"}, {"ROO", "Quintana Roo"},
                {"SLP", "San Luis Potosí"}, {"SIN", "Sinaloa"}, {"SON", "Sonora"}, {"TAB", "Tabasco"},
                {"TAM", "Tamaulipas"}, {"TLA", "Tlaxcala"}, {"VER", "Veracruz"}, {"YUC", "Yucatán"},
                {"ZAC", "Zacatecas"}
            };

            // Mexican States - French
            var mexicanStatesFrench = new Dictionary<string, string>
            {
                {"AGU", "Aguascalientes"}, {"BCN", "Basse-Californie"}, {"BCS", "Basse-Californie du Sud"},
                {"CAM", "Campeche"}, {"CHP", "Chiapas"}, {"CHH", "Chihuahua"}, {"CMX", "Mexico"},
                {"COA", "Coahuila"}, {"COL", "Colima"}, {"DUR", "Durango"}, {"GUA", "Guanajuato"},
                {"GRO", "Guerrero"}, {"HID", "Hidalgo"}, {"JAL", "Jalisco"}, {"MEX", "État de Mexico"},
                {"MIC", "Michoacán"}, {"MOR", "Morelos"}, {"NAY", "Nayarit"}, {"NLE", "Nuevo León"},
                {"OAX", "Oaxaca"}, {"PUE", "Puebla"}, {"QUE", "Querétaro"}, {"ROO", "Quintana Roo"},
                {"SLP", "San Luis Potosí"}, {"SIN", "Sinaloa"}, {"SON", "Sonora"}, {"TAB", "Tabasco"},
                {"TAM", "Tamaulipas"}, {"TLA", "Tlaxcala"}, {"VER", "Veracruz"}, {"YUC", "Yucatán"},
                {"ZAC", "Zacatecas"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in mexicanStatesSpanish)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "es-MX" });
            }

            foreach (var kvp in mexicanStatesEnglish)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "en-MX" });
            }

            foreach (var kvp in mexicanStatesFrench)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "fr-MX" });
            }

            return states;
        }

        /// <summary>
        /// Gets Chinese provinces and autonomous regions in Simplified Chinese and English.
        /// </summary>
        private List<StateProvince> GetChineseProvinces()
        {
            var provinces = new List<StateProvince>();

            // Chinese Provinces - Simplified Chinese (zh-CN)
            var chineseProvincesZhCN = new Dictionary<string, string>
            {
                {"BJ", "北京市"}, {"TJ", "天津市"}, {"HE", "河北省"}, {"SX", "山西省"}, {"NM", "内蒙古自治区"},
                {"LN", "辽宁省"}, {"JL", "吉林省"}, {"HL", "黑龙江省"}, {"SH", "上海市"}, {"JS", "江苏省"},
                {"ZJ", "浙江省"}, {"AH", "安徽省"}, {"FJ", "福建省"}, {"JX", "江西省"}, {"SD", "山东省"},
                {"HA", "河南省"}, {"HB", "湖北省"}, {"HN", "湖南省"}, {"GD", "广东省"}, {"GX", "广西壮族自治区"},
                {"HI", "海南省"}, {"CQ", "重庆市"}, {"SC", "四川省"}, {"GZ", "贵州省"}, {"YN", "云南省"},
                {"XZ", "西藏自治区"}, {"SN", "陕西省"}, {"GS", "甘肃省"}, {"QH", "青海省"}, {"NX", "宁夏回族自治区"},
                {"XJ", "新疆维吾尔自治区"}, {"HK", "香港特别行政区"}, {"MO", "澳门特别行政区"}
            };

            // Chinese Provinces - English
            var chineseProvincesEn = new Dictionary<string, string>
            {
                {"BJ", "Beijing"}, {"TJ", "Tianjin"}, {"HE", "Hebei"}, {"SX", "Shanxi"}, {"NM", "Inner Mongolia"},
                {"LN", "Liaoning"}, {"JL", "Jilin"}, {"HL", "Heilongjiang"}, {"SH", "Shanghai"}, {"JS", "Jiangsu"},
                {"ZJ", "Zhejiang"}, {"AH", "Anhui"}, {"FJ", "Fujian"}, {"JX", "Jiangxi"}, {"SD", "Shandong"},
                {"HA", "Henan"}, {"HB", "Hubei"}, {"HN", "Hunan"}, {"GD", "Guangdong"}, {"GX", "Guangxi"},
                {"HI", "Hainan"}, {"CQ", "Chongqing"}, {"SC", "Sichuan"}, {"GZ", "Guizhou"}, {"YN", "Yunnan"},
                {"XZ", "Tibet"}, {"SN", "Shaanxi"}, {"GS", "Gansu"}, {"QH", "Qinghai"}, {"NX", "Ningxia"},
                {"XJ", "Xinjiang"}, {"HK", "Hong Kong SAR"}, {"MO", "Macau SAR"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in chineseProvincesZhCN)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CN", Culture = "zh-CN" });
            }

            foreach (var kvp in chineseProvincesEn)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CN", Culture = "en-CN" });
            }

            return provinces;
        }

        /// <summary>
        /// Gets Taiwan counties and cities in Traditional Chinese and English.
        /// </summary>
        private List<StateProvince> GetTaiwanRegions()
        {
            var regions = new List<StateProvince>();

            // Taiwan Counties/Cities - Traditional Chinese (zh-TW)
            var taiwanRegionsZhTW = new Dictionary<string, string>
            {
                {"TPE", "臺北市"}, {"TPH", "新北市"}, {"TYC", "桃園市"}, {"TCH", "臺中市"}, {"TNH", "臺南市"},
                {"KHH", "高雄市"}, {"KEE", "基隆市"}, {"HSZ", "新竹市"}, {"HSQ", "新竹縣"}, {"MIA", "苗栗縣"},
                {"CHA", "彰化縣"}, {"NAN", "南投縣"}, {"YUN", "雲林縣"}, {"CYI", "嘉義市"}, {"CYQ", "嘉義縣"},
                {"PIF", "屏東縣"}, {"ILA", "宜蘭縣"}, {"HUA", "花蓮縣"}, {"TTE", "臺東縣"}, {"PEN", "澎湖縣"},
                {"KIN", "金門縣"}, {"LIE", "連江縣"}
            };

            // Taiwan Counties/Cities - English
            var taiwanRegionsEn = new Dictionary<string, string>
            {
                {"TPE", "Taipei City"}, {"TPH", "New Taipei City"}, {"TYC", "Taoyuan City"}, {"TCH", "Taichung City"}, {"TNH", "Tainan City"},
                {"KHH", "Kaohsiung City"}, {"KEE", "Keelung City"}, {"HSZ", "Hsinchu City"}, {"HSQ", "Hsinchu County"}, {"MIA", "Miaoli County"},
                {"CHA", "Changhua County"}, {"NAN", "Nantou County"}, {"YUN", "Yunlin County"}, {"CYI", "Chiayi City"}, {"CYQ", "Chiayi County"},
                {"PIF", "Pingtung County"}, {"ILA", "Yilan County"}, {"HUA", "Hualien County"}, {"TTE", "Taitung County"}, {"PEN", "Penghu County"},
                {"KIN", "Kinmen County"}, {"LIE", "Lienchiang County"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in taiwanRegionsZhTW)
            {
                regions.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "TW", Culture = "zh-TW" });
            }

            foreach (var kvp in taiwanRegionsEn)
            {
                regions.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "TW", Culture = "en-TW" });
            }

            return regions;
        }

        /// <summary>
        /// Gets Egyptian governorates in Arabic and English.
        /// </summary>
        private List<StateProvince> GetEgyptianGovernorates()
        {
            var governorates = new List<StateProvince>();

            // Egyptian Governorates - Arabic (ar-EG)
            var egyptianGovernoratesAr = new Dictionary<string, string>
            {
                {"CAI", "القاهرة"}, {"GIZ", "الجيزة"}, {"SHG", "الشرقية"}, {"DKH", "الدقهلية"}, {"BNS", "بني سويف"},
                {"FYM", "الفيوم"}, {"MNF", "المنوفية"}, {"BHR", "البحيرة"}, {"ISM", "الإسماعيلية"}, {"GH", "الغربية"},
                {"MN", "المنيا"}, {"ASY", "أسيوط"}, {"SWH", "سوهاج"}, {"QN", "قنا"}, {"ASN", "أسوان"},
                {"LX", "الأقصر"}, {"WAD", "الوادي الجديد"}, {"MT", "مطروح"}, {"ALX", "الإسكندرية"}, {"KFS", "كفر الشيخ"},
                {"PTS", "بورسعيد"}, {"DT", "دمياط"}, {"JS", "جنوب سيناء"}, {"SIN", "شمال سيناء"}, {"SUZ", "السويس"},
                {"BA", "البحر الأحمر"}, {"HW", "حلوان"}, {"6O", "السادس من أكتوبر"}
            };

            // Egyptian Governorates - English
            var egyptianGovernoratesEn = new Dictionary<string, string>
            {
                {"CAI", "Cairo"}, {"GIZ", "Giza"}, {"SHG", "Ash Sharqiyah"}, {"DKH", "Dakahlia"}, {"BNS", "Beni Suef"},
                {"FYM", "Fayyum"}, {"MNF", "Monufia"}, {"BHR", "Beheira"}, {"ISM", "Ismailia"}, {"GH", "Gharbia"},
                {"MN", "Minya"}, {"ASY", "Asyut"}, {"SWH", "Sohag"}, {"QN", "Qena"}, {"ASN", "Aswan"},
                {"LX", "Luxor"}, {"WAD", "New Valley"}, {"MT", "Matrouh"}, {"ALX", "Alexandria"}, {"KFS", "Kafr el-Sheikh"},
                {"PTS", "Port Said"}, {"DT", "Damietta"}, {"JS", "South Sinai"}, {"SIN", "North Sinai"}, {"SUZ", "Suez"},
                {"BA", "Red Sea"}, {"HW", "Helwan"}, {"6O", "6th of October"}
            };

            // Create StateProvince objects for each language
            foreach (var kvp in egyptianGovernoratesAr)
            {
                governorates.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "EG", Culture = "ar-EG" });
            }

            foreach (var kvp in egyptianGovernoratesEn)
            {
                governorates.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "EG", Culture = "en-EG" });
            }

            return governorates;
        }
    }
}