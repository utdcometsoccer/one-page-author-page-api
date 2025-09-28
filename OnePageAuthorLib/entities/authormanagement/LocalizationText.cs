namespace InkStainedWretch.OnePageAuthorAPI.Entities.Authormanagement
{
    /// <summary>
    /// Represents all localized text for the author management UI, matching the structure of inkstainedwretch.language-country.json.
    /// </summary>
    public class LocalizationText
    {
        public AuthorRegistration AuthorRegistration { get; set; } = new AuthorRegistration();
        public LoginRegister LoginRegister { get; set; } = new LoginRegister();
        public ThankYou ThankYou { get; set; } = new ThankYou();
        public Navbar Navbar { get; set; } = new Navbar();
        public DomainRegistration DomainRegistration { get; set; } = new DomainRegistration();
        public ErrorPage ErrorPage { get; set; } = new ErrorPage();
        public ImageManager ImageManager { get; set; } = new ImageManager();
        public Checkout Checkout { get; set; } = new Checkout();
        public BookList BookList { get; set; } = new BookList();
        public BookForm BookForm { get; set; } = new BookForm();
        public ArticleForm ArticleForm { get; set; } = new ArticleForm();
        public AuthGuard AuthGuard { get; set; } = new AuthGuard();
    }
}
