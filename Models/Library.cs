using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyComicsManagerApi.Models
{
    public class Library
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string RelPath { get; set; }

        public string getTmpPath()
        {
            return RelPath + "/tmp";
        }

    }
}