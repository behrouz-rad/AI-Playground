using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ImageToText;

namespace AiPlayground.Test.Helpers;

internal static class AiHelper
{
    private const string _ollama_address = "http://localhost:11434";
    private static readonly HttpClient _ollamaClient = new()
    {
        BaseAddress = new Uri(_ollama_address),
        Timeout = TimeSpan.FromMinutes(30)
    };

    private const string _hugging_face_api_address = "https://api-inference.huggingface.co";
    private static readonly HttpClient _huggingFaceClient = new()
    {
        BaseAddress = new Uri(_hugging_face_api_address),
        Timeout = TimeSpan.FromMinutes(30)
    };

    public static async Task SummarizeCodeChangesAsync(CancellationToken ct)
    {
        string appFullPath = Assembly.GetExecutingAssembly().Location;
        string? appFolderPath = Path.GetDirectoryName(appFullPath);

        string modelsFilePath = Path.Combine(appFolderPath!, "assets", "ai_models.txt");
        string[] models = await File.ReadAllLinesAsync(modelsFilePath, Encoding.UTF8, ct);

        string promptFileFullPath = Path.Combine(appFolderPath!, "assets", "prompt.txt");
        string prompt = await File.ReadAllTextAsync(promptFileFullPath, ct);

        string resultPath = Path.Combine(appFolderPath!, "result");

        if (Directory.Exists(resultPath))
        {
            Directory.Delete(resultPath, recursive: true);
        }

        _ = Directory.CreateDirectory(resultPath);

        foreach (string model in models)
        {
            WriteLine($"Started with {model}...");

            DateTime startTime = DateTime.Now;

            Kernel kernel = GetOpenAiKernel(model, apiKey: null, _ollama_address);
            FunctionResult response = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);

            TimeSpan endTime = DateTime.Now - startTime;
            string result = $"It took: {string.Create(CultureInfo.InvariantCulture, $"{endTime}:hh\\:mm\\:ss")} {Environment.NewLine} {response.GetValue<string>()}";
            string resulFiletPath = Path.Combine(resultPath, $"reviewer_{model.Split(':', '.', StringSplitOptions.None)[0]}.txt");

            await File.WriteAllTextAsync(resulFiletPath, result, ct);

            Console.WriteLine($"Done with {model}.");
        }
    }

    public static Task<string?> DescribeImageAsync(ModelMode modelMode, string imagePath, CancellationToken ct)
    {
        return modelMode switch
        {
            ModelMode.Offline => CaptionImageOfflineAsync(
                imagePath,
                "Describe the following image in clear and concise language. Focus on the main elements, such as key objects, colors, and the overall scene, keeping the description under 100 words."
                , ct),
            ModelMode.Online => CaptionImageOnlineAsync(imagePath, ct),
            _ => Task.FromResult<string?>("")
        };
    }

    public static Task<string?> OcrImageAsync(string imagePath, CancellationToken ct)
    {
        return CaptionImageOfflineAsync(
            imagePath,
            "Write only the text from the image to the output and nothing else. If no text is found in the image, write 'NO TEXT DETECTED'.",
            ct);
    }

    private static async Task<string?> CaptionImageOfflineAsync(string imagePath, string prompt, CancellationToken ct)
    {
        byte[] imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        string imageBase64 = Convert.ToBase64String(imageBytes);

        var model = new
        {
            model = "llava:latest",
            prompt,
            images = new[] { imageBase64 },
            stream = false
        };

        string jsonPrompt = JsonSerializer.Serialize(model);

        var content = new StringContent(jsonPrompt, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _ollamaClient.PostAsync("api/generate", content, ct);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(ct);
        }

        return "";
    }

    private static async Task<string?> CaptionImageOnlineAsync(string imagePath, CancellationToken ct)
    {
        string apiKey = await ReadApiKeyAsync(ct);
#pragma warning disable SKEXP0001, SKEXP0070
        Kernel kernel = Kernel.CreateBuilder()
                           .AddHuggingFaceImageToText("Salesforce/blip-image-captioning-large", apiKey: apiKey)
                           .Build();

        IImageToTextService imageToTextService = kernel.GetRequiredService<IImageToTextService>();

        byte[] file = await File.ReadAllBytesAsync(imagePath, ct);
        var imageData = new ReadOnlyMemory<byte>(file);
        ImageContent imageContent = new(imageData, "image/jpeg");

        TextContent textContent = await imageToTextService.GetTextContentAsync(imageContent, cancellationToken: ct);

        return textContent.Text;
    }

    public static async Task<string?> TellStoryAsync(string prompt, CancellationToken ct)
    {
        Kernel kernel = GetOpenAiKernel("orca-mini:latest", apiKey: null, _ollama_address);

        FunctionResult response = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);

        return response.GetValue<string>();
    }

    public static async Task CreateAudioFromStringAsync(string input, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var request = new HttpRequestMessage(HttpMethod.Post, "models/facebook/mms-tts-eng")
        {
            Content = JsonContent.Create(new { inputs = input })
        };

        string apiKey = await ReadApiKeyAsync(ct);
        request.Headers.Add("x-wait-for-model", "true");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");

        HttpResponseMessage response = await _huggingFaceClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            byte[] audioContent = await response.Content.ReadAsByteArrayAsync(ct);

            const string audioFilePath = "output.wav";
            await File.WriteAllBytesAsync(audioFilePath, audioContent, ct);
            Console.WriteLine($"Audio content written to file: {audioFilePath}");
        }
        else
        {
            Console.WriteLine($"Error: {response.ReasonPhrase}");
            string errorContent = await response.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"Response: {errorContent}");
        }
    }

    private static Kernel GetOpenAiKernel(string model, string? apiKey, string endpointAddress)
    {
        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010
        return kernelBuilder
            .AddOpenAIChatCompletion(
                modelId: model,
                endpoint: new Uri(endpointAddress),
                apiKey: apiKey,
                httpClient: _ollamaClient)
            .Build();
    }

    public static void WriteTitle(string title)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(title);
        Console.ResetColor();
    }

    private static Task<string> ReadApiKeyAsync(CancellationToken ct)
    {
        string appFullPath = Assembly.GetExecutingAssembly().Location;
        string? appFolderPath = Path.GetDirectoryName(appFullPath);

        string keyFilePath = Path.Combine(appFolderPath!, "assets", "hg_api_key.txt");
        return File.ReadAllTextAsync(keyFilePath, Encoding.UTF8, ct);
    }
}
