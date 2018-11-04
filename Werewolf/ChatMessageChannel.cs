using System;
using System.Linq;
using System.Collections.Generic;
namespace DNWS.Werewolf
{
    public class ChatMessageChannel
    {
        private List<ChatMessage> _storage;
        public List<ChatMessage> Storage
        {
            get { return _storage;}
        }

        public ChatMessageChannel()
        {
            _storage = new List<ChatMessage>(); 
        }

        public void Add(ChatMessage m)
        {
            lock (_storage)
            {
                long? max_id = _storage.Max(message => message.Id);
                if (max_id == null)
                {
                    max_id = 0;
                }
                else
                {
                    max_id += 1;
                }
                m.Id = max_id;
                _storage.Append<ChatMessage>(m);
            }
        } 

        public List<ChatMessage> GetSince(string id)
        {
            long lid = long.Parse(id);
            List<ChatMessage> messages = _storage.Where(message => message.Id > lid).OrderBy(message => message.Id).ToList<ChatMessage>();
            return messages;
        }
    }
}