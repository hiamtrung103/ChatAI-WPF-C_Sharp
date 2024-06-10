using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace WPF_UI
{
    public class ChatDatabaseSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string ThreadsCollectionName { get; set; }
    }

    public class Thread
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Title { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message
    {
        public string Content { get; set; }
        public string Sender { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
