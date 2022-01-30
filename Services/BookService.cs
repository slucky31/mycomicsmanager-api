using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Globalization;
using MyComicsManagerApi.DataParser;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class BookService
    {
        private static ILogger Log => Serilog.Log.ForContext<BookService>();
        
        private readonly IMongoCollection<Book> _books;
        private readonly GoogleBooksApiDataService _googleBooksApiDataService;
        
        public BookService(IDatabaseSettings dbSettings, GoogleBooksApiDataService googleBooksApiDataService)
        {
            Log.Here().Debug("settings = {@Settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _books = database.GetCollection<Book>(dbSettings.BooksCollectionName);
            _googleBooksApiDataService = googleBooksApiDataService;
        }

        public List<Book> Get() =>
            _books.Find(book => true).ToList();

        public Book Get(string id) =>
            _books.Find(book => book.Id == id).FirstOrDefault();

        public Book Create(Book bookIn)
        {
            // Création de la librairie dans MangoDB
            _books.InsertOne(bookIn);
            return bookIn;
        }

        public void Update(string id, Book bookIn) {
            _books.ReplaceOne(book => book.Id == id, bookIn);
        }

        public void Remove(Book bookIn)
        {
            // Suppression de la référence en base de données
            _books.DeleteOne(book => book.Id == bookIn.Id);
        }
        
        public Book SearchComicInfoAndUpdate(string isbn)
        {
            if (string.IsNullOrEmpty(isbn))
            {
                return null;
            }

            var book = new Book();

            var parser = new BdphileComicHtmlDataParser();
            var results = parser.Parse(isbn);

            if (results.Count == 1)
            {
                book.Isbn = results[ComicDataEnum.ISBN];
                book.Serie = results[ComicDataEnum.SERIE];
                book.Title = results[ComicDataEnum.TITRE];
                var frCulture = new CultureInfo("fr-FR");
            
                if (int.TryParse(results[ComicDataEnum.TOME], out var intValue))
                {
                    book.Volume = intValue;
                }
                else
                {
                    Log.Warning("Une erreur est apparue lors de l'analyse du volume : {Tome}",
                        results[ComicDataEnum.TOME]);
                }
            }
            else
            {
                var bookInfo = _googleBooksApiDataService.GetBookInformation(isbn);
                if (bookInfo.Result != null && bookInfo.Result.Items.Count > 0)
                {
                    book.Isbn = isbn;
                    book.Title = bookInfo.Result.Items[0].VolumeInfo.Title;
                }
            }
            
            
            
            Update(book.Id, book);
            return book;
        }
        
    }
}