namespace MyComicsManagerApi.Models
{
    public class DatabaseSettings : IDatabaseSettings
    {
        public string ComicsCollectionName { get; set; }
        public string BooksCollectionName { get; set; }
        public string LibrariesCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IDatabaseSettings
    {
        string ComicsCollectionName { get; set; }
        string BooksCollectionName { get; set; }
        string LibrariesCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}