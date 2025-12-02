using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMSpeech.Core.Plugins;

namespace TMSpeech.Translator.OpenAI;

public class OpenAITranslator : ITranslator
{
    public string GUID => "F47AC10B-58CC-4372-A567-0E02B2C3D479";
    public string Name => "OpenAI翻译器";
    public string Description => "使用OpenAI API进行实时翻译";
    public string Version => "0.0.1";
    public string SupportVersion => "any";
    public string Author => "Built-in";
    public string Url => "";
    public string License => "MIT License";
    public string Note => "";
    
    public IPluginConfigEditor CreateConfigEditor() => new OpenAIConfigEditor();
    
    private OpenAIConfig _config = new OpenAIConfig();
    private HttpClient _httpClient = new HttpClient();
    
    public void LoadConfig(string config)
    {
        if (!string.IsNullOrEmpty(config))
        {
            try
            {
                _config = JsonSerializer.Deserialize<OpenAIConfig>(config) ?? new OpenAIConfig();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize OpenAI config: {ex.Message}");
                _config = new OpenAIConfig();
            }
        }
    }
    
    public bool Available => !string.IsNullOrEmpty(_config.ApiKey) && !string.IsNullOrEmpty(_config.Model);
    
    public void Init()
    {
        // 初始化逻辑
    }
    
    public void Destroy()
    {
        // 清理逻辑
        _httpClient.Dispose();
    }
    
    public string Translate(string text)
    {
        Trace.WriteLine($"OpenAITranslator: 开始翻译文本: {text}");
        if (string.IsNullOrEmpty(text) || !Available)
        {
            Trace.WriteLine($"OpenAITranslator: 跳过翻译，文本为空或翻译器不可用");
            return text;
        }
        
        try
        {
            // 使用Task.Run将异步操作包装为同步操作，并使用ConfigureAwait(false)避免死锁
            var result = Task.Run(async () => await TranslateAsync(text).ConfigureAwait(false)).Result;
            Trace.WriteLine($"OpenAITranslator: 翻译完成，结果: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"OpenAITranslator: 翻译失败: {ex.Message}");
            Console.WriteLine($"翻译失败: {ex.Message}");
            return text;
        }
    }
    
    private async Task<string> TranslateAsync(string text)
    {
        Trace.WriteLine($"OpenAITranslator: 开始异步翻译文本: {text}");
        
        try
        {
            var requestBody = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "system", content = $"你是一个翻译助手，将用户输入翻译成{_config.TargetLanguage}。请直接输出翻译结果，不要添加任何解释或额外内容。" },
                    new { role = "user", content = text }
                },
                temperature = 0.7
            };
            
            Trace.WriteLine($"OpenAITranslator: 准备发送请求到API，模型: {_config.Model}，目标语言: {_config.TargetLanguage}");
            
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );
            
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
            
            var response = await _httpClient.PostAsync(_config.ApiUrl, content);
            response.EnsureSuccessStatusCode();
            
            Trace.WriteLine($"OpenAITranslator: API请求成功，状态码: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            Trace.WriteLine($"OpenAITranslator: API响应: {responseBody}");
            
            var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody);
            
            var translatedText = result?.choices?[0]?.message?.content ?? text;
            Trace.WriteLine($"OpenAITranslator: 异步翻译完成，结果: {translatedText}");
            
            return translatedText;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"OpenAITranslator: 异步翻译失败: {ex.Message}");
            throw;
        }
    }
}

public class OpenAIConfig
{
    public string ApiKey { get; set; } = "ollama";
    public string ApiUrl { get; set; } = "http://192.168.11.84:11434/v1/chat/completions";
    public string Model { get; set; } = "nothink4";
    public string TargetLanguage { get; set; } = "中文";
}

public class OpenAIResponse
{
    public Choice[] choices { get; set; }
}

public class Choice
{
    public Message message { get; set; }
}

public class Message
{
    public string content { get; set; }
}
