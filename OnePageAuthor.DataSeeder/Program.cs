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
    /// Seeds US, Canada, and Mexico data in English, French, Spanish, Arabic, Simplified Chinese, and Traditional Chinese.
    /// Implements idempotent operations - can be run multiple times safely.
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
                    // Standardize configuration keys and require explicit configuration for production safety
                    var cosmosEndpoint = configuration["COSMOSDB_ENDPOINT_URI"] 
                        ?? configuration["CosmosDb:Endpoint"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_ENDPOINT")
                        ?? throw new InvalidOperationException("COSMOSDB_ENDPOINT_URI is required. For development, you can use the emulator endpoint: https://localhost:8081");
                    
                    var cosmosKey = configuration["COSMOSDB_PRIMARY_KEY"]
                        ?? configuration["CosmosDb:Key"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_KEY")
                        ?? throw new InvalidOperationException("COSMOSDB_PRIMARY_KEY is required. For development, you can use the emulator key: C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");
                    
                    var databaseId = configuration["COSMOSDB_DATABASE_ID"]
                        ?? configuration["CosmosDb:Database"]
                        ?? Environment.GetEnvironmentVariable("COSMOS_DB_DATABASE")
                        ?? throw new InvalidOperationException("COSMOSDB_DATABASE_ID is required");

                    // Log configuration (masked for security)
                    Console.WriteLine("Starting StateProvince Data Seeding...");
                    Console.WriteLine($"Cosmos DB Endpoint configured: {Utility.MaskUrl(cosmosEndpoint)}");
                    Console.WriteLine($"Cosmos DB Database ID configured: {databaseId}");

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
        /// This operation is idempotent - it can be run multiple times safely without creating duplicates.
        /// </summary>
        public async Task SeedDataAsync()
        {
            _logger.LogInformation("Starting idempotent StateProvince data seeding...");

            var allData = new List<StateProvince>();

            // Add North American countries data in all supported languages
            allData.AddRange(GetUSStates());
            allData.AddRange(GetCanadianProvinces());
            allData.AddRange(GetMexicanStates());

            _logger.LogInformation("Preparing to seed {Count} StateProvince entries...", allData.Count);

            int createdCount = 0;
            int skippedCount = 0;
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
                        errorCount++;
                        continue;
                    }

                    // Create unique ID for each culture variant
                    stateProvince.id = $"{stateProvince.Code}_{stateProvince.Culture}";

                    // Check if entry already exists (idempotent operation)
                    var existing = await _stateProvinceService.GetStateProvinceByCountryCultureAndCodeAsync(
                        stateProvince.Country!, stateProvince.Culture!, stateProvince.Code!);

                    if (existing != null)
                    {
                        // Entry already exists, skip it
                        skippedCount++;
                        _logger.LogDebug("Skipped (already exists): {Code} - {Name} ({Culture})", 
                            stateProvince.Code, stateProvince.Name, stateProvince.Culture);
                    }
                    else
                    {
                        // Entry doesn't exist, create it
                        await _stateProvinceService.CreateStateProvinceAsync(stateProvince);
                        createdCount++;
                        _logger.LogDebug("Created: {Code} - {Name} ({Culture})", 
                            stateProvince.Code, stateProvince.Name, stateProvince.Culture);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Failed to process StateProvince: {Code} - {Name} ({Culture})",
                        stateProvince.Code, stateProvince.Name, stateProvince.Culture);
                }
            }

            _logger.LogInformation("Idempotent data seeding completed. Created: {CreatedCount}, Skipped: {SkippedCount}, Errors: {ErrorCount}", 
                createdCount, skippedCount, errorCount);
        }

        /// <summary>
        /// Gets US states and territories in English, French, Spanish, Arabic, Simplified Chinese, and Traditional Chinese.
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

            // US States - Arabic
            var usStatesArabic = new Dictionary<string, string>
            {
                {"AL", "ألاباما"}, {"AK", "ألاسكا"}, {"AZ", "أريزونا"}, {"AR", "أركنساس"},
                {"CA", "كاليفورنيا"}, {"CO", "كولورادو"}, {"CT", "كونيتيكت"}, {"DE", "ديلاوير"},
                {"FL", "فلوريدا"}, {"GA", "جورجيا"}, {"HI", "هاواي"}, {"ID", "أيداهو"},
                {"IL", "إلينوي"}, {"IN", "إنديانا"}, {"IA", "آيوا"}, {"KS", "كانساس"},
                {"KY", "كنتاكي"}, {"LA", "لويزيانا"}, {"ME", "مين"}, {"MD", "ماريلاند"},
                {"MA", "ماساتشوستس"}, {"MI", "ميشيغان"}, {"MN", "مينيسوتا"}, {"MS", "ميسيسيبي"},
                {"MO", "ميزوري"}, {"MT", "مونتانا"}, {"NE", "نبراسكا"}, {"NV", "نيفادا"},
                {"NH", "نيوهامبشير"}, {"NJ", "نيوجيرسي"}, {"NM", "نيومكسيكو"}, {"NY", "نيويورك"},
                {"NC", "كارولاينا الشمالية"}, {"ND", "داكوتا الشمالية"}, {"OH", "أوهايو"}, {"OK", "أوكلاهوما"},
                {"OR", "أوريغون"}, {"PA", "بنسلفانيا"}, {"RI", "رود آيلاند"}, {"SC", "كارولاينا الجنوبية"},
                {"SD", "داكوتا الجنوبية"}, {"TN", "تينيسي"}, {"TX", "تكساس"}, {"UT", "يوتا"},
                {"VT", "فيرمونت"}, {"VA", "فرجينيا"}, {"WA", "واشنطن"}, {"WV", "فرجينيا الغربية"},
                {"WI", "ويسكونسن"}, {"WY", "وايومنغ"}, {"DC", "مقاطعة كولومبيا"},
                {"AS", "ساموا الأمريكية"}, {"GU", "غوام"}, {"MP", "جزر ماريانا الشمالية"},
                {"PR", "بورتوريكو"}, {"VI", "جزر فيرجن"}
            };

            // US States - Simplified Chinese
            var usStatesSimplifiedChinese = new Dictionary<string, string>
            {
                {"AL", "阿拉巴马州"}, {"AK", "阿拉斯加州"}, {"AZ", "亚利桑那州"}, {"AR", "阿肯色州"},
                {"CA", "加利福尼亚州"}, {"CO", "科罗拉多州"}, {"CT", "康涅狄格州"}, {"DE", "特拉华州"},
                {"FL", "佛罗里达州"}, {"GA", "乔治亚州"}, {"HI", "夏威夷州"}, {"ID", "爱达荷州"},
                {"IL", "伊利诺伊州"}, {"IN", "印第安纳州"}, {"IA", "爱荷华州"}, {"KS", "堪萨斯州"},
                {"KY", "肯塔基州"}, {"LA", "路易斯安那州"}, {"ME", "缅因州"}, {"MD", "马里兰州"},
                {"MA", "马萨诸塞州"}, {"MI", "密歇根州"}, {"MN", "明尼苏达州"}, {"MS", "密西西比州"},
                {"MO", "密苏里州"}, {"MT", "蒙大拿州"}, {"NE", "内布拉斯加州"}, {"NV", "内华达州"},
                {"NH", "新罕布什尔州"}, {"NJ", "新泽西州"}, {"NM", "新墨西哥州"}, {"NY", "纽约州"},
                {"NC", "北卡罗来纳州"}, {"ND", "北达科他州"}, {"OH", "俄亥俄州"}, {"OK", "俄克拉荷马州"},
                {"OR", "俄勒冈州"}, {"PA", "宾夕法尼亚州"}, {"RI", "罗德岛州"}, {"SC", "南卡罗来纳州"},
                {"SD", "南达科他州"}, {"TN", "田纳西州"}, {"TX", "德克萨斯州"}, {"UT", "犹他州"},
                {"VT", "佛蒙特州"}, {"VA", "弗吉尼亚州"}, {"WA", "华盛顿州"}, {"WV", "西弗吉尼亚州"},
                {"WI", "威斯康星州"}, {"WY", "怀俄明州"}, {"DC", "哥伦比亚特区"},
                {"AS", "美属萨摩亚"}, {"GU", "关岛"}, {"MP", "北马里亚纳群岛"},
                {"PR", "波多黎各"}, {"VI", "美属维尔京群岛"}
            };

            // US States - Traditional Chinese
            var usStatesTraditionalChinese = new Dictionary<string, string>
            {
                {"AL", "阿拉巴馬州"}, {"AK", "阿拉斯加州"}, {"AZ", "亞利桑那州"}, {"AR", "阿肯色州"},
                {"CA", "加利福尼亞州"}, {"CO", "科羅拉多州"}, {"CT", "康涅狄格州"}, {"DE", "特拉華州"},
                {"FL", "佛羅里達州"}, {"GA", "喬治亞州"}, {"HI", "夏威夷州"}, {"ID", "愛達荷州"},
                {"IL", "伊利諾伊州"}, {"IN", "印第安納州"}, {"IA", "愛荷華州"}, {"KS", "堪薩斯州"},
                {"KY", "肯塔基州"}, {"LA", "路易斯安那州"}, {"ME", "緬因州"}, {"MD", "馬里蘭州"},
                {"MA", "麻薩諸塞州"}, {"MI", "密歇根州"}, {"MN", "明尼蘇達州"}, {"MS", "密西西比州"},
                {"MO", "密蘇里州"}, {"MT", "蒙大拿州"}, {"NE", "內布拉斯加州"}, {"NV", "內華達州"},
                {"NH", "新罕布什爾州"}, {"NJ", "新澤西州"}, {"NM", "新墨西哥州"}, {"NY", "紐約州"},
                {"NC", "北卡羅來納州"}, {"ND", "北達科他州"}, {"OH", "俄亥俄州"}, {"OK", "俄克拉荷馬州"},
                {"OR", "俄勒岡州"}, {"PA", "賓夕法尼亞州"}, {"RI", "羅德島州"}, {"SC", "南卡羅來納州"},
                {"SD", "南達科他州"}, {"TN", "田納西州"}, {"TX", "德克薩斯州"}, {"UT", "猶他州"},
                {"VT", "佛蒙特州"}, {"VA", "弗吉尼亞州"}, {"WA", "華盛頓州"}, {"WV", "西弗吉尼亞州"},
                {"WI", "威斯康星州"}, {"WY", "懷俄明州"}, {"DC", "哥倫比亞特區"},
                {"AS", "美屬薩摩亞"}, {"GU", "關島"}, {"MP", "北馬里亞納群島"},
                {"PR", "波多黎各"}, {"VI", "美屬維爾京群島"}
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

            foreach (var kvp in usStatesArabic)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "ar-US" });
            }

            foreach (var kvp in usStatesSimplifiedChinese)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "zh-CN" });
            }

            foreach (var kvp in usStatesTraditionalChinese)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "US", Culture = "zh-TW" });
            }

            return states;
        }

        /// <summary>
        /// Gets Canadian provinces and territories in English, French, Spanish, Arabic, Simplified Chinese, and Traditional Chinese.
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

            // Canadian Provinces - Spanish
            var canadianProvincesSpanish = new Dictionary<string, string>
            {
                {"AB", "Alberta"}, {"BC", "Columbia Británica"}, {"MB", "Manitoba"},
                {"NB", "Nuevo Brunswick"}, {"NL", "Terranova y Labrador"}, {"NS", "Nueva Escocia"},
                {"ON", "Ontario"}, {"PE", "Isla del Príncipe Eduardo"}, {"QC", "Quebec"},
                {"SK", "Saskatchewan"}, {"NT", "Territorios del Noroeste"}, {"NU", "Nunavut"},
                {"YT", "Yukón"}
            };

            // Canadian Provinces - Arabic
            var canadianProvincesArabic = new Dictionary<string, string>
            {
                {"AB", "ألبرتا"}, {"BC", "كولومبيا البريطانية"}, {"MB", "مانيتوبا"},
                {"NB", "نيو برونزويك"}, {"NL", "نيوفاوندلاند ولابرادور"}, {"NS", "نوفا سكوتيا"},
                {"ON", "أونتاريو"}, {"PE", "جزيرة الأمير إدوارد"}, {"QC", "كيبك"},
                {"SK", "ساسكاتشوان"}, {"NT", "الأقاليم الشمالية الغربية"}, {"NU", "نونافوت"},
                {"YT", "يوكون"}
            };

            // Canadian Provinces - Simplified Chinese
            var canadianProvincesSimplifiedChinese = new Dictionary<string, string>
            {
                {"AB", "艾伯塔省"}, {"BC", "不列颠哥伦比亚省"}, {"MB", "马尼托巴省"},
                {"NB", "新不伦瑞克省"}, {"NL", "纽芬兰与拉布拉多省"}, {"NS", "新斯科舍省"},
                {"ON", "安大略省"}, {"PE", "爱德华王子岛省"}, {"QC", "魁北克省"},
                {"SK", "萨斯喀彻温省"}, {"NT", "西北地区"}, {"NU", "努纳武特地区"},
                {"YT", "育空地区"}
            };

            // Canadian Provinces - Traditional Chinese
            var canadianProvincesTraditionalChinese = new Dictionary<string, string>
            {
                {"AB", "艾伯塔省"}, {"BC", "不列顛哥倫比亞省"}, {"MB", "馬尼托巴省"},
                {"NB", "新不倫瑞克省"}, {"NL", "紐芬蘭與拉布拉多省"}, {"NS", "新斯科舍省"},
                {"ON", "安大略省"}, {"PE", "愛德華王子島省"}, {"QC", "魁北克省"},
                {"SK", "薩斯喀徹溫省"}, {"NT", "西北地區"}, {"NU", "努納武特地區"},
                {"YT", "育空地區"}
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

            foreach (var kvp in canadianProvincesSpanish)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "es-CA" });
            }

            foreach (var kvp in canadianProvincesArabic)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "ar-CA" });
            }

            foreach (var kvp in canadianProvincesSimplifiedChinese)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "zh-CN" });
            }

            foreach (var kvp in canadianProvincesTraditionalChinese)
            {
                provinces.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "CA", Culture = "zh-TW" });
            }

            return provinces;
        }

        /// <summary>
        /// Gets Mexican states in Spanish, English, French, Arabic, Simplified Chinese, and Traditional Chinese.
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

            // Mexican States - Arabic
            var mexicanStatesArabic = new Dictionary<string, string>
            {
                {"AGU", "أغواسكالينتيس"}, {"BCN", "باخا كاليفورنيا"}, {"BCS", "باخا كاليفورنيا الجنوبية"},
                {"CAM", "كامبيتشي"}, {"CHP", "تشياباس"}, {"CHH", "تشيواوا"}, {"CMX", "مدينة مكسيكو"},
                {"COA", "كواويلا"}, {"COL", "كوليما"}, {"DUR", "دورانغو"}, {"GUA", "غواناخواتو"},
                {"GRO", "غيريرو"}, {"HID", "هيدالغو"}, {"JAL", "خاليسكو"}, {"MEX", "ولاية المكسيك"},
                {"MIC", "ميتشواكان"}, {"MOR", "موريلوس"}, {"NAY", "ناياريت"}, {"NLE", "نويفو ليون"},
                {"OAX", "أواخاكا"}, {"PUE", "بويبلا"}, {"QUE", "كيريتارو"}, {"ROO", "كينتانا رو"},
                {"SLP", "سان لويس بوتوسي"}, {"SIN", "سينالوا"}, {"SON", "سونورا"}, {"TAB", "تاباسكو"},
                {"TAM", "تاماوليباس"}, {"TLA", "تلاكسكالا"}, {"VER", "فيراكروز"}, {"YUC", "يوكاتان"},
                {"ZAC", "زاكاتيكاس"}
            };

            // Mexican States - Simplified Chinese
            var mexicanStatesSimplifiedChinese = new Dictionary<string, string>
            {
                {"AGU", "阿瓜斯卡连特斯州"}, {"BCN", "下加利福尼亚州"}, {"BCS", "南下加利福尼亚州"},
                {"CAM", "坎佩切州"}, {"CHP", "恰帕斯州"}, {"CHH", "奇瓦瓦州"}, {"CMX", "墨西哥城"},
                {"COA", "科阿韦拉州"}, {"COL", "科利马州"}, {"DUR", "杜兰戈州"}, {"GUA", "瓜纳华托州"},
                {"GRO", "格雷罗州"}, {"HID", "伊达尔戈州"}, {"JAL", "哈利斯科州"}, {"MEX", "墨西哥州"},
                {"MIC", "米却肯州"}, {"MOR", "莫雷洛斯州"}, {"NAY", "纳亚里特州"}, {"NLE", "新莱昂州"},
                {"OAX", "瓦哈卡州"}, {"PUE", "普埃布拉州"}, {"QUE", "克雷塔罗州"}, {"ROO", "金塔纳罗奥州"},
                {"SLP", "圣路易斯波托西州"}, {"SIN", "锡那罗亚州"}, {"SON", "索诺拉州"}, {"TAB", "塔巴斯科州"},
                {"TAM", "塔毛利帕斯州"}, {"TLA", "特拉斯卡拉州"}, {"VER", "韦拉克鲁斯州"}, {"YUC", "尤卡坦州"},
                {"ZAC", "萨卡特卡斯州"}
            };

            // Mexican States - Traditional Chinese
            var mexicanStatesTraditionalChinese = new Dictionary<string, string>
            {
                {"AGU", "阿瓜斯卡連特斯州"}, {"BCN", "下加利福尼亞州"}, {"BCS", "南下加利福尼亞州"},
                {"CAM", "坎佩切州"}, {"CHP", "恰帕斯州"}, {"CHH", "奇瓦瓦州"}, {"CMX", "墨西哥城"},
                {"COA", "科阿韋拉州"}, {"COL", "科利馬州"}, {"DUR", "杜蘭戈州"}, {"GUA", "瓜納華托州"},
                {"GRO", "格雷羅州"}, {"HID", "伊達爾戈州"}, {"JAL", "哈利斯科州"}, {"MEX", "墨西哥州"},
                {"MIC", "米卻肯州"}, {"MOR", "莫雷洛斯州"}, {"NAY", "納亞里特州"}, {"NLE", "新萊昂州"},
                {"OAX", "瓦哈卡州"}, {"PUE", "普埃布拉州"}, {"QUE", "克雷塔羅州"}, {"ROO", "金塔納羅奧州"},
                {"SLP", "聖路易斯波托西州"}, {"SIN", "錫那羅亞州"}, {"SON", "索諾拉州"}, {"TAB", "塔巴斯科州"},
                {"TAM", "塔毛利帕斯州"}, {"TLA", "特拉斯卡拉州"}, {"VER", "韋拉克魯斯州"}, {"YUC", "尤卡坦州"},
                {"ZAC", "薩卡特卡斯州"}
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

            foreach (var kvp in mexicanStatesArabic)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "ar-MX" });
            }

            foreach (var kvp in mexicanStatesSimplifiedChinese)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "zh-CN" });
            }

            foreach (var kvp in mexicanStatesTraditionalChinese)
            {
                states.Add(new StateProvince { Code = kvp.Key, Name = kvp.Value, Country = "MX", Culture = "zh-TW" });
            }

            return states;
        }
    }
}