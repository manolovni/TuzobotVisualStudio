using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Net.Http;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;

namespace Tuzobot
{
    [Serializable]
    public class MainDialog : IDialog<object>
    {
        bool menu = false;
        bool con = false;
        bool an = false;
        bool admin = false;
        bool loginFlag = true;
        bool conStart = false;
        string broadcastText = "";
        DateTime contestEndDate = DateTime.Now;
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> argument)
        {
            
            var message = await argument;

            if (admin && conStart)
            {
                if (DateTime.TryParse(message.Text, out contestEndDate))
                {
                    PromptDialog.Confirm(
                        context,
                        ContestStartFinal,
                        $"Запускаем конкурс:\n\n{ broadcastText}\n\n Завершение:{contestEndDate.ToString()} \n\nВсе правильно?",
                        "Не понимаю, отправим всем или нет?",
                        promptStyle: PromptStyle.None);
                }
                else
                {
                    var reply = context.MakeMessage();
                    reply.Text = "Извини, не понимаю 😏\n\n Когда закончится конкурс?";
                    reply.Attachments = DateSelect();

                    await context.PostAsync(reply);
                }
                return;
            }


            if (message.Text.Contains("выход"))
            {
                loginFlag = false;
                menu = false;
                admin = false;
                await context.PostAsync("До скорого!");
                context.Wait(MessageReceivedAsync);
                return;
            }

            if (con)
            {
                broadcastText = message.Text;
                PromptDialog.Confirm(
                    context,
                    ContestStart,
                    $"Ты хочешь отправить всем такое описание конкурса:\n\n{ message.Text}\n\n Правильно?",
                    "Не понимаю, отправим всем или нет?",
                    promptStyle: PromptStyle.None);
                return;
            }

            if (an)
            {
                    broadcastText = message.Text;
                    PromptDialog.Confirm(
                        context,
                        BroadcastAsync,
                        $"Ты хочешь отправить всем:\n\n{ message.Text}\n\n Правильно?",
                        "Не понимаю, отправим всем или нет?",
                        promptStyle: PromptStyle.None);
                    return;
            }

            if (menu)
            {
                if (message.Text == "res")
                {
                    con = true;
                    var db = new TuzobotModelContainer();

                    int userCount = db.ConvSet.Count();
                    int contestCount = db.ContestSet.Where(c => c.Active).Count();
                    int activeSubmissions = db.SubmitSet.Count(s => s.Contest.Active);
                    await context.PostAsync($"Оперативная сводка:\n\n Активных переписок в рассылке - {userCount}\n\nАктивных конкурсов - {contestCount}\n\nЗаявок на текущий конкурс - {activeSubmissions}");
                    context.Wait(MessageReceivedAsync);
                }

                if (message.Text == "an")
                {
                    an = true;
                    await context.PostAsync("Точно, давай сделаем анонс, что напишем?");
                    context.Wait(MessageReceivedAsync);
                }

                if (message.Text == "con")
                {
                    con = true;
                    await context.PostAsync("Супер, новый конкурс!\n\nОпиши его для ребят, не забудь про призы, это очень важно 😊");
                    context.Wait(MessageReceivedAsync);
                }

                if (message.Text == "end")
                {
                    PromptDialog.Confirm(
                        context,
                        EndContestBeforeDue,
                        $"Завершаем конкурс досрочно?",
                        "Не понимаю, завершаем или нет?",
                        promptStyle: PromptStyle.None);
                    return;
                }

                menu = false;
                return;
            }

            if (message.Text == "123" && loginFlag)
            {
                //adminConvId = message.ConversationId;
                admin = true;
                menu = true;
                var reply = context.MakeMessage();
                reply.Text = "Добро пожаловать, хозяин!\n\nЧего изволите?";
                reply.Attachments = AdminMenu();

                await context.PostAsync(reply);
                context.Wait(MessageReceivedAsync);
                return;
            }

            if (message.Text.Contains("Я админ"))
            {
                //if (message.ConversationId == "COGGhF5OhEapApPG9ZDdlCYgBKf2RBAmu96o9cP6Tj5tb4Gv")
                //{
                    loginFlag = true;
                    await context.PostAsync("Чем докажешь?");
                    context.Wait(MessageReceivedAsync);
                    return;
                //}
            }

            if (message.Attachments.Count > 0)
            {
                CV cv = new CV();
                TuzobotModelContainer db = new TuzobotModelContainer();
                foreach (var a in message.Attachments)
                {
                    Emotion[] emotions = await cv.Detect(a.ContentUrl);
                    if (emotions != null && emotions.Length > 0)
                    {
                        var happyDelta = ((double)emotions[0].Scores.Happiness - 0.5);
                        var surpriseDelta = ((double)emotions[0].Scores.Surprise - 0.5);
                        Contest contest = db.ContestSet.SingleOrDefault(c => c.Active);

                        if (contest == null)
                        {
                            contest = db.ContestSet.SingleOrDefault(c => c.Id == 2);
                        }
                        else
                        {
                            await context.PostAsync($"Твоя фотография принята на конкурс, но можно сделать еще лучше 🌟🌟🌟🌟");
                        }

                        Conv conv = db.ConvSet.SingleOrDefault(c => c.ConversationId == message.ConversationId);
                        var newSubmit = new Submit();
                        newSubmit.UserName = message.From.Name;
                        newSubmit.IsNotAdult = true;
                        newSubmit.IsWinner = false;
                        newSubmit.Conv = conv;
                        newSubmit.Image = message.Attachments[0].ContentUrl;
                        newSubmit.Contest = contest;
                        newSubmit.Score = happyDelta * happyDelta + surpriseDelta * surpriseDelta;
                        db.SubmitSet.Add(newSubmit);
                        db.SaveChanges();

                        await context.PostAsync($"Счастье {(100*emotions[0].Scores.Happiness).ToString("N2")}%");
                        await context.PostAsync($"Удивление {(100 * emotions[0].Scores.Surprise).ToString("N2")}%");
                        await context.PostAsync($"Испуг {(100 * emotions[0].Scores.Fear).ToString("N2")}%");

)
                    }
                    else
                    {
                        await context.PostAsync($"К сожаленю, не смог распознать эмоцию, попробуй еще 😊");
                    }
                }
            }


                await context.PostAsync("Я умею распознавать эмоции на фотографиях, пришли мне слефи 😘");
                context.Wait(MessageReceivedAsync);
        }

        public async Task EndContestBeforeDue(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Конкурс завершен, подтвердите результаты перед отправкой промокодов.");
                var db = new TuzobotModelContainer();
                var winners = db.SubmitSet.Where(s => s.Contest.Active).OrderBy(s=>s.Score).Take(3);
                var m = context.MakeMessage();
                //m.Attachments = new List<Attachment>();
                foreach(var w in winners)
                {
                //    m.Attachments.Add(new Attachment()
                //    {
                //        ContentUrl = w.Image,
                //        ContentType = "image/png"
                //    });
                await context.PostAsync($"![{w.UserName}]({w.Image})");
                }

                PromptDialog.Confirm(
                    context,
                    Finish,
                    $"Завершаем конкурс?",
                    "Не понимаю, завершаем или нет?",
                    promptStyle: PromptStyle.None);

                return;

            }
            else
            {
                await context.PostAsync("Конкурс продолжается!");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task BroadcastAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                
                Broadcast(broadcastText);
                await context.PostAsync("🌟🌟🌟🌟\n\nАнонс отправляется!");
            }
            else
            {
                await context.PostAsync("Придумаем еще что-нибудь.");
            }
            an = false;
            context.Wait(MessageReceivedAsync);
        }

        private List<Attachment> AdminMenu()
        {
            var result = new List<Attachment>();
            var actions = new List<Microsoft.Bot.Connector.Action>();
            actions.Add(new Microsoft.Bot.Connector.Action
            {
                Title = $"Результаты",
                Message = $"res"
            });
            actions.Add(new Microsoft.Bot.Connector.Action
            {
                Title = $"Анонс",
                Message = $"an"
            });

            var db = new TuzobotModelContainer();
            var contest = db.ContestSet.Where(c => c.Active).SingleOrDefault();
            if (contest == null)
            {
                actions.Add(new Microsoft.Bot.Connector.Action
                {
                    Title = $"Конкурс",
                    Message = $"con"
                });
            }
            else
            {
                actions.Add(new Microsoft.Bot.Connector.Action
                {
                    Title = $"Завершить конкурс",
                    Message = $"end"
                });
            }
            result.Add(new Attachment
            {
                Actions = actions
            });
            menu = true;
            return result;
        }

        public async void Broadcast(string text)
        {
            var db = new TuzobotModelContainer();
            foreach (var c in db.ConvSet)
            {
                Task.Factory.StartNew(() => SendMessage(c, text));
            }
        }

        public async Task SendMessage(Conv c, string Text)
        {
            string appId = System.Configuration.ConfigurationManager.AppSettings["appId"];
            string appSecret = System.Configuration.ConfigurationManager.AppSettings["appSecret"];
            string url = "https://api.botframework.com/bot/v1.0/messages";
            using (var client = new HttpClient())
            {
                var content = new StringContent(
                $"{{\"conversationId\": \"{c.ConversationId}\",\"text\": \"{Text}\",\"from\": {{\"channelId\": \"{c.ChannelId}\",\"address\": \"{c.BotAddress}\"}},\"to\": {{\"channelId\": \"{c.ChannelId}\",\"address\": \"{c.UserAddress}\"}}}}",
                Encoding.UTF8,
                "application/json");
                var byteArray = Encoding.ASCII.GetBytes($"{appId}:{appSecret}");
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", appSecret);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        public async Task ContestStart(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                conStart = true;

                var reply = context.MakeMessage();
                reply.Text = "🔥🔥🔥\n\nАнонс готов!\n\nКогда закончится конкурс?";
                reply.Attachments = DateSelect();

                await context.PostAsync(reply);
            }
            else
            {
                var reply = context.MakeMessage();
                reply.Text = "Ок, тогда что-нибудь еще придумаем.";
                reply.Attachments = AdminMenu();

                await context.PostAsync(reply);
            }
            con = false;
            context.Wait(MessageReceivedAsync);
        }

        public async Task Finish(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {

                var reply = context.MakeMessage();
                reply.Text = "🔥🔥🔥\n\nУра!\n\nКонкурс завершен, промокоды отправлены победителям!";
                await context.PostAsync(reply);

                var db = new TuzobotModelContainer();
                var winners = db.SubmitSet.Where(s => s.Contest.Active).OrderBy(s => s.Score).Take(3);
                var m = context.MakeMessage();
                //m.Attachments = new List<Attachment>();
                await context.PostAsync($"\n\nПобедители!");
                foreach (var w in winners)
                {
                    var promo = CreatePassword(6);

                    w.IsWinner = true;
                    w.Promoceode = promo;
                    await context.PostAsync($"{w.UserName} - {promo}");
                }
                var lastWinners = db.SubmitSet.Where(s => s.IsWinner && s.Contest.Active);
                var cont = db.ContestSet.SingleOrDefault(c => c.Active);
                cont.Active = false;
                db.SaveChanges();

                foreach(var w in lastWinners)
                {
                    SendMessage(w.Conv,$"Поздравляем!\n\nВы победили в конкурсе!\n\nВаш промокод: {w.Promoceode}")
                }


            }
            else
            {
                var reply = context.MakeMessage();
                reply.Text = "Ок, тогда что-нибудь еще придумаем.";
                reply.Attachments = AdminMenu();

                await context.PostAsync(reply);
            }
            con = false;
            context.Wait(MessageReceivedAsync);
        }

        private List<Attachment> DateSelect()
        {
            var result = new List<Attachment>();
            var actions = new List<Microsoft.Bot.Connector.Action>();
            actions.Add(new Microsoft.Bot.Connector.Action
            {
                Title = $"Через два часа",
                Message = DateTime.Now.AddHours(2).ToString()
            });
            actions.Add(new Microsoft.Bot.Connector.Action
            {
                Title = $"Сегодня 12:00",
                Message = DateTime.Today.AddHours(12).ToString()
            });
            actions.Add(new Microsoft.Bot.Connector.Action
            {
                Title = $"Завтра 12:00",
                Message = DateTime.Today.AddDays(1).AddHours(12).ToString()
            });
            result.Add(new Attachment
            {
                Actions = actions
            });
            return result;
        }

        public async Task ContestStartFinal(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {

                var db = new TuzobotModelContainer();
                var contests = db.ContestSet.Where(c => c.Active).SingleOrDefault();
                if (contests == null)
                {
                    Contest c = new Contest();
                    c.Active = true;
                    c.EndDate = contestEndDate;
                    c.Description = broadcastText;
                    c.NumberOfWinners = 3;
                    c.Type = 1;
                    c.Image = "";
                    
                    db.ContestSet.Add(c);
                    db.SaveChanges();
                    var reply = context.MakeMessage();
                    reply.Text = "🔥🔥🔥\n\nКонкурс начинается!\n\nРассылка пошла, как закончу - отпишусь.";

                    await context.PostAsync(reply);
                    foreach (var con in db.ConvSet)
                    {
                        SendMessage(con, $"{ broadcastText}\n\nПодача заявок до {contestEndDate}");
                    }
                }
                else
                {
                    var reply = context.MakeMessage();
                    reply.Text = "В сситеме уже идет конкурс";
                    await context.PostAsync(reply);
                }
            }
            else
            {
                var reply = context.MakeMessage();
                reply.Text = "Ок, тогда что-нибудь еще придумаем.";
                reply.Attachments = AdminMenu();

                await context.PostAsync(reply);
            }
            con = false;
            context.Wait(MessageReceivedAsync);
        }

        public string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

    }
}