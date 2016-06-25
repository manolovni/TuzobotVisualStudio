using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ProjectOxford.Emotion;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion.Contract;

namespace Tuzobot
{
    public class CV
    {
        EmotionServiceClient emotion = new EmotionServiceClient("b8a2c940bad3484793e5f5c0a2303d67");
        public async Task<Emotion[]> Detect(string url)
        {
            try
            {
                //
                // Detect the emotions in the URL
                //
                Emotion[] emotionResult = await emotion.RecognizeAsync(url);
                return emotionResult;
            }
            catch
            {
                return null;
            }
        }
    }
}