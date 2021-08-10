using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManagerApi.Models
{
    public class Comic
    {
        
        // Technical data
        
        [BsonId]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EbookPath { get; set; }

        public string EbookName { get; set; }

        public string LibraryId { get; set; }

        public string CoverPath { get; set; }

        // Book info

        public string Serie { get; set; }
        
        public string Title { get; set; }

        public string ISBN { get; set; }

        public string Volume { get; set; }

        public string Summary { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; }

        public DateTime Published { get; set; }

        public string Writer { get; set; }

        public string Penciller { get; set; }

        public string Colorist { get; set; }

        public string Editor { get; set; }

        public string LanguageISO { get; set; }
        
        public int PageCount { get; set; }

        public int Review { get; set; }

        public string FicheUrl { get; set; }

    }
}