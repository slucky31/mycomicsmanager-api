using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManagerApi.Models
{
    public class Book
    {
        
        // Technical data
        
        [BsonId]
        [BsonIgnoreIfDefault]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Book info

        public string Serie { get; set; }
        
        public string Title { get; set; }

        public string Isbn { get; set; }

        public int Volume { get; set; }
        
        public DateTime Added { get; set; }
        
        public int Review { get; set; }

    }
}