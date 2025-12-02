using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMSpeech.Core.Plugins
{
    public class TextInfo
    {
        public DateTime Time { get; set; }
        public string TimeStr => Time.ToString("T");
        public string Text { get; set; }
        public string TranslatedText { get; set; } = string.Empty;
        public TextInfo(string text)
        {
            Time = DateTime.Now;
            Text = ToTitleCase(text);
        }
        
        /// <summary>
        /// 将文本转换为标准的首字母大写格式
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>首字母大写的文本</returns>
        private string ToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // 首先将所有字符转换为小写
            text = text.ToLower();
            
            // 将第一个字符转换为大写
            char[] chars = text.ToCharArray();
            if (char.IsLetter(chars[0]))
            {
                chars[0] = char.ToUpper(chars[0]);
            }
            
            // 将句子开头的字符转换为大写（以句号、问号、感叹号结尾的句子）
            for (int i = 1; i < chars.Length; i++)
            {
                if ((chars[i - 1] == '.' || chars[i - 1] == '?' || chars[i - 1] == '!' || chars[i - 1] == '。' || chars[i - 1] == '？' || chars[i - 1] == '！') && i + 1 < chars.Length)
                {
                    // 跳过空格
                    int j = i;
                    while (j < chars.Length && char.IsWhiteSpace(chars[j]))
                    {
                        j++;
                    }
                    if (j < chars.Length && char.IsLetter(chars[j]))
                    {
                        chars[j] = char.ToUpper(chars[j]);
                    }
                }
            }
            
            return new string(chars);
        }
    }

    public class SpeechEventArgs
    {
        public TextInfo Text { get; set; }
    }

    public interface IRecognizer : IPlugin, IRunable
    {
        event EventHandler<SpeechEventArgs> TextChanged;
        event EventHandler<SpeechEventArgs> SentenceDone;

        /// <summary>
        /// Feed audio data to the recognizer (e.g. from a microphone or a file
        /// </summary>
        /// <param name="data"></param>
        void Feed(byte[] data);
    }
}
