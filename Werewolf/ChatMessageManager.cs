using System;
using System.Linq;
using System.Collections.Generic;
namespace DNWS.Werewolf
{
    public class ChatMessageManager
    {
        public void Add(ChatMessage m)
        {
            using (ChatMessageContext _context = new ChatMessageContext())
            {
                m.Id = null;
                _context.ChatMessages.Add(m);
                _context.SaveChanges();
                Console.WriteLine("Add message id {0} to channel {1} from user {2}", m.Id, m.Channel, m.PlayerId);
            }
        } 

        public List<ChatMessage> GetSince(long gameid, ChatMessage.ChannelEnum channel, string id)
        {
            long lid = long.Parse(id);
            using (ChatMessageContext _context = new ChatMessageContext())
            {
                List<ChatMessage> messages = _context.ChatMessages.Where(message => message.GameId == gameid && message.Channel == channel && message.Id >= lid).OrderBy(message => message.Id).ToList<ChatMessage>();
                Console.WriteLine("Found {0} from channel {1}", messages.Count, channel);
                return messages;
            }
        }
    }
}