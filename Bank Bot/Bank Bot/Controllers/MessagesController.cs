using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Microsoft.WindowsAzure.MobileServices;

using System.Collections.Generic;

using Bank_Bot.Models;


namespace Bank_Bot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        private readonly string GOOGLE_API_KEY = "AIzaSyChi5FEeYth9XEkb531DkF7WtecOLm2V_s";

        private readonly string SENT_GREETING = "SentGreeting";
        private readonly string INSULT_STRIKES = "InsultStrikes";
        private readonly string SEARCHINGFORBANK = "FindingBank";
        private readonly string LOGGED_IN = "logged in";
        private readonly string PERSONAL_ACCOUNT = "personal Account";
        private readonly string POTENTIAL_PAYEE = "potentialPayee";
        private readonly string POTENTIAL_PAYEE_AMOUNT = "amount";

        private readonly string CONFIRMED_PAYMENT = "WEGWivpMc7ynhlPFzQx2";
        private readonly string CANCELLED_PAYMENT = "Q1GQ1JLzszTBJ8RBUHdQ";

        private readonly string BANK_IMAGE_URL = "https://s11.postimg.org/n1mf1hleb/Custom_Bank_Branding.jpg";
        private readonly string TICK_IMAGE_URL = "http://www.clipartkid.com/images/228/correct-tick-clipart-best-zljug2-clipart.png";
        private readonly string CROSS_IMAGE_URL = "http://www.clker.com/cliparts/1/1/9/2/12065738771352376078Arnoud999_Right_or_wrong_5.svg.med.png";
        private readonly string MAPS_IMAGE_URL = "https://s17.postimg.org/o0b6xo8sv/Bank_Location.png";

        private readonly string LUIS_URL = "https://api.projectoxford.ai/luis/v2.0/apps/9dfd3e25-ebc1-41c6-955a-66f9497a9539?subscription-key=cf6eebd33695450f962472fa4215406e&q=";
        private readonly string GOOGLE_URL = "https://maps.googleapis.com/maps/api/directions/json?origin=";
        private readonly string GOOGLE_MAPS_URL = "https://www.google.co.nz/maps/dir/";

        private readonly string BANK_LOCATION = "1 Queen Street, Auckland";

        private readonly string NONE = "None";
        private readonly string GREETING = "greeting";
        private readonly string PAYMENT = "payment";
        private readonly string APPOINTMENT = "makeAppointment";
        private readonly string CHECK_BALANCE = "check Balance";
        private readonly string FIND = "find";
        private readonly string INSULT = "insult";
        private readonly string MORE_INFORMATION = "Website";
        private readonly string LOG_OUT = "Log out";
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
                LUISObj.RootObject LUISRoot;
                MapObj.RootObject mapRoot;

                var userMessage = activity.Text;
                string endOutput = "";

                MobileServiceClient msclient = AzureManager.AzureManagerInstance.AzureClient;
                HttpClient client = new HttpClient();

                if (!userData.GetProperty<bool>(LOGGED_IN))
                {
                    Activity reply = activity.CreateReply($"You are not logged in. Please enter your username");

                    BankAccountInformation information = await AzureManager.AzureManagerInstance.getAccountFromName(userMessage);
                    if(information != null)
                    {
                        userData.SetProperty<BankAccountInformation>(PERSONAL_ACCOUNT, information);
                        userData.SetProperty<bool>(LOGGED_IN, true);

                        string name = userData.GetProperty<BankAccountInformation>(PERSONAL_ACCOUNT).Name;
                        reply = activity.CreateReply($"You have logged in successfully. Welcome {name}");
                        await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                    }

                    await connector.Conversations.ReplyToActivityAsync(reply);
                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                if(userMessage == CONFIRMED_PAYMENT)
                {
                    Activity reply = activity.CreateReply("Processing your transaction...");
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    reply = activity.CreateReply("still processing...");

                    //update things and confirm payment
                    string amount = userData.GetProperty<string>(POTENTIAL_PAYEE_AMOUNT);
                    string payee = userData.GetProperty<string>(POTENTIAL_PAYEE);
                    string payer = userData.GetProperty<BankAccountInformation>(PERSONAL_ACCOUNT).Name;
                    BankAccountInformation personalAccount = userData.GetProperty<BankAccountInformation>(PERSONAL_ACCOUNT);
                    BankAccountInformation temp = await AzureManager.AzureManagerInstance.getAccountFromName(payee);
                    double actualAmount = Convert.ToDouble(amount);

                    personalAccount.Money = personalAccount.Money - actualAmount;
                      temp.Money = temp.Money + actualAmount;

                   // personalAccount.password = "Greetings";

                    await AzureManager.AzureManagerInstance.UpdateTimeline(personalAccount);
                    await AzureManager.AzureManagerInstance.UpdateTimeline(temp);
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    reply = activity.CreateReply($"payment of ${amount} to {payee} has been confirmed");
                    await connector.Conversations.ReplyToActivityAsync(reply);

                    userData.SetProperty<string>(POTENTIAL_PAYEE, "");
                    userData.SetProperty<string>(POTENTIAL_PAYEE_AMOUNT, "");

                    return Request.CreateResponse(HttpStatusCode.OK);

                } else if (userMessage == CANCELLED_PAYMENT)
                {
                    userData.SetProperty<string>(POTENTIAL_PAYEE, "");
                    userData.SetProperty<string>(POTENTIAL_PAYEE_AMOUNT, "");
                    Activity reply = activity.CreateReply($"Payment cancelled");
                    await connector.Conversations.ReplyToActivityAsync(reply);


                    return Request.CreateResponse(HttpStatusCode.OK);

                }

                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                if (userData.GetProperty<bool>(SEARCHINGFORBANK))
                {
                    string y = await client.GetStringAsync(new Uri(GOOGLE_URL + userMessage + "&destination=" + BANK_LOCATION + "&key=" + GOOGLE_API_KEY));
                    mapRoot = JsonConvert.DeserializeObject<MapObj.RootObject>(y);

                    Activity PaymentConversation = activity.CreateReply();
                    PaymentConversation.Recipient = activity.From;
                    PaymentConversation.Type = "message";
                    PaymentConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    //change to google maps image
                    cardImages.Add(new CardImage(url: MAPS_IMAGE_URL));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction viewOnWebBtn = new CardAction()
                    {
                        Image = BANK_IMAGE_URL,
                        Value = GOOGLE_MAPS_URL + userMessage + "/" + BANK_LOCATION,
                        Type = "openUrl",
                        Title = "View on Google Maps"
                    };
                    cardButtons.Add(viewOnWebBtn);

                    HeroCard confirmPaymentCard = new HeroCard()
                    {
                        Title = "Route",
                        Subtitle = "Your nearest bank is at " + BANK_LOCATION 
                        + ". It is " + mapRoot.routes[0].legs[0].duration.text + " away.",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment confirmPaymentAttachment = confirmPaymentCard.ToAttachment();
                    PaymentConversation.Attachments.Add(confirmPaymentAttachment);
                    await connector.Conversations.SendToConversationAsync(PaymentConversation);

                    userData.SetProperty<bool>(SEARCHINGFORBANK, false);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    return Request.CreateResponse(HttpStatusCode.OK);
                }

                string x = await client.GetStringAsync(new Uri(LUIS_URL
                    + userMessage + "&timezoneOffset=12.0&verbose=true"));

                LUISRoot = JsonConvert.DeserializeObject<LUISObj.RootObject>(x);


                string intent = LUISRoot.topScoringIntent.intent;

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
                    var action = LUISRoot.topScoringIntent.actions[0];
                    //display a card with a confirm button, the amount, the payee
                    if (action.parameters[0].value == null && action.parameters[1].value == null)
                    {
                        Activity testingReply = activity.CreateReply($"You require either a contact name or an account number to pay");
                        await connector.Conversations.ReplyToActivityAsync(testingReply);

                        return Request.CreateResponse(HttpStatusCode.OK);
                    }

                    string luckyBugger;
                    endOutput = null;

                    if (action.parameters[0].value == null)
                    {
                        luckyBugger = action.parameters[1].value[0].entity;
                        BankAccountInformation information = await AzureManager.AzureManagerInstance.getAccountFromNumber(luckyBugger);
                        if (information == null)
                        {
                            endOutput = "The number " + luckyBugger + " does not exist in this database.";
                        }
                    } else
                    {
                        luckyBugger = action.parameters[0].value[0].entity;

                        BankAccountInformation information = await AzureManager.AzureManagerInstance.getAccountFromName(luckyBugger);
                        if (information == null)
                        {
                            endOutput = "The name " + luckyBugger + " does not exist in this database.";
                        }
                    }

                    if(endOutput != null)
                    {
                        Activity reply = activity.CreateReply(endOutput);
                        await connector.Conversations.ReplyToActivityAsync(reply);

                        return Request.CreateResponse(HttpStatusCode.OK);
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
                        Value = CONFIRMED_PAYMENT,
                        Type = "postBack",
                        Title = "confirm"
                    };
                    cardButtons.Add(confirmButton);

                    CardAction cancelBtn = new CardAction()
                    {
                        Image = CROSS_IMAGE_URL,
                        Value = CANCELLED_PAYMENT,
                        Type = "postBack",
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

                    userData.SetProperty<string>(POTENTIAL_PAYEE, luckyBugger);
                    userData.SetProperty<string>(POTENTIAL_PAYEE_AMOUNT, action.parameters[2].value[0].entity);
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

                    Attachment confirmPaymentAttachment = confirmPaymentCard.ToAttachment();
                    PaymentConversation.Attachments.Add(confirmPaymentAttachment);
                    await connector.Conversations.SendToConversationAsync(PaymentConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);
                } else if (intent == APPOINTMENT)
                {
                    //display a list of appointment
                } else if (intent == CHECK_BALANCE)
                {
                    double moneyAmount = userData.GetProperty<BankAccountInformation>(PERSONAL_ACCOUNT).Money;
                    Activity bankStatement = activity.CreateReply("You have $" + moneyAmount + " in your bank account");
                    await connector.Conversations.ReplyToActivityAsync(bankStatement);
                } else if (intent == LOG_OUT)
                {

                    await stateClient.BotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);
                    Activity reply = activity.CreateReply($"log out successfull");
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else if (intent == FIND)
                {
                    //use GPS co-ordinate system to find the nearest bank
                    //use a card to display a map, and text saying the distance

                    userData.SetProperty<bool>(SEARCHINGFORBANK, true);

                    Activity reply = activity.CreateReply($"Please enter your current address");
                    await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);

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
                } else if (intent == MORE_INFORMATION)
                {
                    Activity PaymentConversation = activity.CreateReply();
                    PaymentConversation.Recipient = activity.From;
                    PaymentConversation.Type = "message";
                    PaymentConversation.Attachments = new List<Attachment>();

                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: BANK_IMAGE_URL));

                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction travelBtn = new CardAction()
                    {
                        Value = "http://msa.ms",
                        Type = "openUrl",
                        Title = "Go to website"
                    };
                    cardButtons.Add(travelBtn);

                    ThumbnailCard confirmPaymentCard = new ThumbnailCard()
                    {
                        Title = "Website",
                        Subtitle = "Visit our website for more information",
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment confirmPaymentAttachment = confirmPaymentCard.ToAttachment();
                    PaymentConversation.Attachments.Add(confirmPaymentAttachment);
                    await connector.Conversations.SendToConversationAsync(PaymentConversation);

                    return Request.CreateResponse(HttpStatusCode.OK);

                }
                else if (intent == NONE)
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