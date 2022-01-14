using MyComicsManagerApi.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using MyComicsManagerApi.Utils;
using Serilog;

namespace MyComicsManagerApi.Services
{
    public class BookService
    {
        private static ILogger Log => Serilog.Log.ForContext<BookService>();
        
        private readonly IMongoCollection<Book> _books;
        
        public BookService(IDatabaseSettings dbSettings)
        {
            Log.Here().Debug("settings = {@Settings}", dbSettings);
            var client = new MongoClient(dbSettings.ConnectionString);
            var database = client.GetDatabase(dbSettings.DatabaseName);
            _books = database.GetCollection<Book>(dbSettings.BooksCollectionName);
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





    }
}