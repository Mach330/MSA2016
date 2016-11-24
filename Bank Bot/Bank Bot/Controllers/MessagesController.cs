using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

using System.Collections.Generic;

using Bank_Bot.Models;


namespace Bank_Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        private readonly string SENT_GREETING = "SentGreeting";
        private readonly string INSULT_STRIKES = "InsultStrikes";
        private readonly string SEARCHINGFORBANK = "FindingBank";

        private readonly string BANK_IMAGE_URL = "https://s11.postimg.org/n1mf1hleb/Custom_Bank_Branding.jpg";
        private readonly string TICK_IMAGE_URL = "http://www.clipartkid.com/images/228/correct-tick-clipart-best-zljug2-clipart.png";
        private readonly string CROSS_IMAGE_URL = "http://www.clker.com/cliparts/1/1/9/2/12065738771352376078Arnoud999_Right_or_wrong_5.svg.med.png";

        private readonly string BANK_LOCATION = "1 Queen Street, Auckland";

        private readonly string NONE = "None";
        private readonly string GREETING = "greeting";
        private readonly string PAYMENT = "payment";
        private readonly string APPOINTMENT = "makeAppointment";
        private readonly string CHECK_BALANCE = "check Balance";
        private readonly string FIND = "find";
        private readonly string INSULT = "insult";
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                LUISObj.RootObject rootObject;
                var userMessage = activity.Text;

                if (userData.GetProperty<bool>(SEARCHINGFORBANK))
                {

                }

                HttpClient client = new HttpClient();
                string x = await client.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/9dfd3e25-ebc1-41c6-955a-66f9497a9539?subscription-key=cf6eebd33695450f962472fa4215406e&q=" 
                    + userMessage + "&timezoneOffset=12.0&verbose=true"));

                rootObject = JsonConvert.DeserializeObject<LUISObj.RootObject>(x);

                string intent = rootObject.topScoringIntent.intent;

                if (intent == GREETING)
                {

                    Activity greetingReply;

                    if (userData.GetProperty<bool>(SENT_GREETING))
                    {
                        greetingReply = activity.CreateReply($"Hello again");
                    }
                    else
                    {
                        userData.SetProperty<bool>(SENT_GREETING, true);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        greetingReply = activity.CreateReply($"Hello! What can I help you with?");
                    }

                    connector.Conversations.ReplyToActivity(greetingReply);
                } else if (intent == PAYMENT)
                {
                    var action = rootObject.topScoringIntent.actions[0];
                    //display a card with a confirm button, the amount, the payee
                    if (action.parameters[0].value == null && action.parameters[1].value == null)
                    {
                        Activity testingReply = activity.CreateReply($"You require either a contact or an account number to pay");
                        await connector.Conversations.ReplyToActivityAsync(testingReply);

                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    string luckyBugger;

                    if (action.parameters[0].value == null)
                    {
                        luckyBugger = action.parameters[1].value[0].entity;
                    } else
                    {
                        luckyBugger = action.parameters[0].value[0].entity;
                    }

                    Activity PaymentConversation = activity.CreateReply();
                    PaymentConversation.Recipient = activity.From;
                    PaymentConversation.Type = "message";
                    PaymentConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: BANK_IMAGE_URL));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction confirmButton = new CardAction()
                    {
                        Image = TICK_IMAGE_URL,
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "confirm"
                    };
                    cardButtons.Add(confirmButton);

                    CardAction cancelBtn = new CardAction()
                    {
                        Image = CROSS_IMAGE_URL,
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "cancel"
                    };
                    cardButtons.Add(cancelBtn);

                    ThumbnailCard confirmPaymentCard = new ThumbnailCard()
                    {
                        Title = "Confirm Payment",
                        Subtitle = "Please confirm you want to pay $" + action.parameters[2].value[0].entity + " to "
                        + luckyBugger,
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment confirmPaymentAttachment = confirmPaymentCard.ToAttachment();
                    PaymentConversation.Attachments.Add(confirmPaymentAttachment);
                    await connector.Conversations.SendToConversationAsync(PaymentConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                } else if (intent == APPOINTMENT)
                {
                    //display a list of appointment
                } else if (intent == CHECK_BALANCE)
                {
                    double moneyAmount = 20.01;
                    Activity bankStatement = activity.CreateReply("You have $" + moneyAmount + "in your bank account");
                } else if (intent == FIND)
                {
                    //use GPS co-ordinate system to find the nearest bank
                    //use a card to display a map, and text saying the distance

                    userData.SetProperty<bool>(SEARCHINGFORBANK, true);

                    Activity reply = activity.CreateReply($"Please enter your current address");
                    await connector.Conversations.SendToConversationAsync(reply);
                } else if (intent == INSULT)
                {

                    Activity insultReply;

                    if(userData.GetProperty<int>(INSULT_STRIKES) == 3)
                    {
                        //use a card or automatically call the helpline
                        insultReply = activity.CreateReply($"Would you like to call our helpline?");
                    } else
                    {
                        userData.SetProperty<int>(INSULT_STRIKES, userData.GetProperty<int>(INSULT_STRIKES) + 1);
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        insultReply = activity.CreateReply($"Please watch your language");
                    }

                    await connector.Conversations.ReplyToActivityAsync(insultReply);
                } else if (intent == NONE)
                {
                    Activity reply = activity.CreateReply($"Sorry I do not understand what you're saying");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                // return our reply to the user
              //  Activity reply = activity.CreateReply($"The intent is " + rootObject.topScoringIntent.intent);
               // await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}