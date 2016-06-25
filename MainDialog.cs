using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Net.Http;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace Tuzobot
{
    [Serializable]
    public class MainDialog : IDialog<object>
    {
        TuzobotModelContainer db = new TuzobotModelContainer();
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }
        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<Message> argument)
        {
            
            var message = await argument;

            if (message.Attachments.Count > 0)
            {
                CV cv = new CV();
                foreach (var a in message.Attachments)
                {
                    Emotion[] emotions = await cv.Detect(a.ContentUrl);
                    if (emotions != null && emotions.Length > 0)
                    {
                        var happyDelta = ((double)emotions[0].Scores.Happiness - 0.5);
                        var surpriseDelta = ((double)emotions[0].Scores.Surprise - 0.5);
                        Contest contest = db.ContestSet.SingleOrDefault(c=>c.Active);
                        if (contest == null) contest = db.ContestSet.SingleOrDefault(c => c.Id == 2);
                        Conv conv = db.ConvSet.SingleOrDefault(c=>c.ConversationId == message.ConversationId);
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
                this.count = 1;
                await context.PostAsync("Reset count.");
            }
            else
            {
                await context.PostAsync("Did not reset count.");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}