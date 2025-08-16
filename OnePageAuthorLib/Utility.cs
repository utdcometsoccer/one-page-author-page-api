namespace InkStainedWretch.OnePageAuthorAPI
{
    public static class Utility
    {
        public static string GetDataRoot()
        {
            string path = "data";
            return GetAbsolutePath(path);
        }

        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", path));
        }
    }
}
