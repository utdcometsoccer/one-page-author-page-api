namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    /// <summary>
    /// Represents all localized text for the author management UI, matching the structure of inkstainedwretch.language-country.json.
    /// </summary>
    public class LocalizationText
    {
        public App App { get; set; } = new App();
        public ArticleForm ArticleForm { get; set; } = new ArticleForm();
        public ArticleList ArticleList { get; set; } = new ArticleList();
        public AuthGuard AuthGuard { get; set; } = new AuthGuard();
        public AuthorDocList AuthorDocList { get; set; } = new AuthorDocList();
        public AuthorMainForm AuthorMainForm { get; set; } = new AuthorMainForm();
        public AuthorRegistration AuthorRegistration { get; set; } = new AuthorRegistration();
        public BookForm BookForm { get; set; } = new BookForm();
        public BookList BookList { get; set; } = new BookList();
        public Checkout Checkout { get; set; } = new Checkout();
        public ChooseCulture ChooseCulture { get; set; } = new ChooseCulture();
        public ChooseSubscription ChooseSubscription { get; set; } = new ChooseSubscription();
        public CountdownIndicator CountdownIndicator { get; set; } = new CountdownIndicator();
        public DomainInput DomainInput { get; set; } = new DomainInput();
        public DomainRegistration DomainRegistration { get; set; } = new DomainRegistration();
        public DomainRegistrationsList DomainRegistrationsList { get; set; } = new DomainRegistrationsList();
        public ErrorBoundary ErrorBoundary { get; set; } = new ErrorBoundary();
        public ErrorPage ErrorPage { get; set; } = new ErrorPage();
        public ImageManager ImageManager { get; set; } = new ImageManager();
        public LoginRegister LoginRegister { get; set; } = new LoginRegister();
        public Navbar Navbar { get; set; } = new Navbar();
        public OpenLibraryAuthorForm OpenLibraryAuthorForm { get; set; } = new OpenLibraryAuthorForm();
        public PenguinRandomHouseAuthorDetail PenguinRandomHouseAuthorDetail { get; set; } = new PenguinRandomHouseAuthorDetail();
        public PenguinRandomHouseAuthorList PenguinRandomHouseAuthorList { get; set; } = new PenguinRandomHouseAuthorList();
        public ProgressIndicator ProgressIndicator { get; set; } = new ProgressIndicator();
        public SocialForm SocialForm { get; set; } = new SocialForm();
        public SocialList SocialList { get; set; } = new SocialList();
        public ThankYou ThankYou { get; set; } = new ThankYou();
        public Toast Toast { get; set; } = new Toast();
        public ToastMessages ToastMessages { get; set; } = new ToastMessages();
        public WelcomePage WelcomePage { get; set; } = new WelcomePage();
    }
}
