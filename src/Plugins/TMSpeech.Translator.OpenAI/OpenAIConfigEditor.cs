using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TMSpeech.Core.Plugins;

namespace TMSpeech.Translator.OpenAI;

public class OpenAIConfigEditor : IPluginConfigEditor
{
    private OpenAIConfig _config = new OpenAIConfig();
    
    public IReadOnlyList<PluginConfigFormItem> GetFormItems()
    {
        return new PluginConfigFormItem[]
        {
            new PluginConfigFormItemPassword("apiKey", "API密钥", "OpenAI API密钥"),
            new PluginConfigFormItemText("apiUrl", "API地址", "OpenAI API地址", "https://api.openai.com/v1/chat/completions"),
            new PluginConfigFormItemText("model", "模型名称", "OpenAI模型名称", "gpt-3.5-turbo"),
            new PluginConfigFormItemText("targetLanguage", "目标语言", "翻译目标语言", "英语")
        };
    }
    
    public event EventHandler<EventArgs>? FormItemsUpdated;
    
    public IReadOnlyDictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>
        {
            { "apiKey", _config.ApiKey },
            { "apiUrl", _config.ApiUrl },
            { "model", _config.Model },
            { "targetLanguage", _config.TargetLanguage }
        };
    }
    
    public void SetValue(string key, object value)
    {
        switch (key)
        {
            case "apiKey":
                _config.ApiKey = value.ToString();
                break;
            case "apiUrl":
                _config.ApiUrl = value.ToString();
                break;
            case "model":
                _config.Model = value.ToString();
                break;
            case "targetLanguage":
                _config.TargetLanguage = value.ToString();
                break;
        }
    }
    
    public object GetValue(string key)
    {
        switch (key)
        {
            case "apiKey":
                return _config.ApiKey;
            case "apiUrl":
                return _config.ApiUrl;
            case "model":
                return _config.Model;
            case "targetLanguage":
                return _config.TargetLanguage;
            default:
                return string.Empty;
        }
    }
    
    public event EventHandler<EventArgs>? ValueUpdated;
    
    public string GenerateConfig()
    {
        return JsonSerializer.Serialize(_config);
    }
    
    public void LoadConfigString(string config)
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
}