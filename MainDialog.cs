using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Net.Http;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Linq;
using System.Collections.Generic;

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

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> argument)
        {
            
            var message = await argument;

            if (message.Text.Contains("выход"))
            {
                loginFlag = false;
                menu = false;
                admin = false;
                await context.PostAsync("До скорого!");
                context.Wait(MessageReceivedAsync);
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
                    await context.PostAsync($"Оперативная сводка:\n\n Активных переписок в рассылке - {userCount}\n\nАктивных конкурсов - {contestCount}");
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
                if (message.ConversationId == "COGGhF5OhEapApPG9ZDdlCYgBKf2RBAmu96o9cP6Tj5tb4Gv")
                {
                    loginFlag = true;
                    await context.PostAsync("Чем докажешь?");
                    context.Wait(MessageReceivedAsync);
                    return;
                }
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
                        if (contest == null) contest = db.ContestSet.SingleOrDefault(c => c.Id == 2);
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
                    }
                    else
                    {
                        await context.PostAsync($"К сожаленю, не смог распознать эмоцию, попробуй еще 😊");
                    }
                }
            }

            if (message.Text == "reset")
            {
                PromptDialog.Confirm(
                    context,
                    AfterResetAsync,
                    "Are you sure you want to reset the count?",
                    "Didn't get that!",
                    promptStyle: PromptStyle.None);
            }
            else
            {
                await context.PostAsync("Я умею распознавать эмоции на фотографиях, пришли мне слефи 😘");
                context.Wait(MessageReceivedAsync);
            }
        }
        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task EndContestBeforeDue(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                await context.PostAsync("Конкурс завершен, подтвердите результаты перед отправкой промокодов.");
            }
            else
            {
                await context.PostAsync("Конкурс продолжается!");
            }
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
    }
}