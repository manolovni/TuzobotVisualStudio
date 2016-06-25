using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Linq;
using System;

namespace Tuzobot
{
    //[BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            

            if (message.Type == "Message")
            {
                AddConversationToDb(message);

                return await Conversation.SendAsync(message, () => new MainDialog());
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }

        private void AddConversationToDb(Message message)
        {
            var db = new TuzobotModelContainer();
            var conv = db.ConvSet.SingleOrDefault(c=>c.ConversationId == message.ConversationId);
            if(conv==null)
            {
                Conv newConversation = new Conv();
                newConversation.ConversationId = message.ConversationId;
                newConversation.UserAddress = message.From.Address;
                newConversation.BotAddress = message.To.Address;
                newConversation.LastActive = DateTime.UtcNow;
                newConversation.ChannelId = message.From.ChannelId;
                db.ConvSet.Add(newConversation);
            }
            else
            {
                conv.LastActive = DateTime.UtcNow;
            }
            db.SaveChanges();
        }
    }
}