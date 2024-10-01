using System.Text.Json;
using AiPlayground.Test.Helpers;

var cts = new CancellationTokenSource();
CancelKeyPress += (sender, eventArgs) =>
{
    WriteLine("Canceling...");
    cts.Cancel();
    eventArgs.Cancel = true;
};

// Summarizing code changes
//await AiHelper.SummarizeCodeChangesAsync(cts.Token);

//WriteLine();

string appFullPath = Assembly.GetExecutingAssembly().Location;
string? appFolderPath = Path.GetDirectoryName(appFullPath);
string imageRecognitionFullPath = Path.Combine(appFolderPath!, "Assets", "image_recognition.jpg");

// Text recognition
//AiHelper.WriteTitle("Text recognition is in process...");

//string textRecognitionFullPath = Path.Combine(appFolderPath!, "Assets", "text_recognition.jpg");
//string? textRecognitionResponse = await AiHelper.OcrImageAsync(textRecognitionFullPath, cts.Token);
//AiResponse? textRecognitionModel = JsonSerializer.Deserialize<AiResponse>(textRecognitionResponse) ?? new AiResponse();
//WriteLine(textRecognitionModel.Response);
//WriteLine();

// Image recognition using Hugging Face
//AiHelper.WriteTitle("Image recognition using Hugging Face is in process...");

//string? imageRecognition1Response = await AiHelper.DescribeImageAsync(ModelMode.Online, imageRecognitionFullPath, cts.Token);
//WriteLine(imageRecognition1Response);
//WriteLine();

// Image recognition using Ollama
AiHelper.WriteTitle("Image recognition using Ollama is in process...");

string? imageRecognitionResponse = await AiHelper.DescribeImageAsync(ModelMode.Offline, imageRecognitionFullPath, cts.Token);
AiResponse? imageRecognitionModel = JsonSerializer.Deserialize<AiResponse>(imageRecognitionResponse) ?? new AiResponse();
WriteLine(imageRecognitionModel.Response);
WriteLine();

// Telling a story
AiHelper.WriteTitle("Writing a story is in process...");

string storyPrompt = $"""
  Here’s a brief description of events. Please expand it into a well-written and engaging story with vivid details, dialogue, and character development. Feel free to add any missing elements that would make it more compelling:
  {imageRecognitionModel}
  Make sure the story has a clear beginning, middle, and end, and follows a logical flow. The tone should be lighthearted and the length should be less than 200 words.
  """;
string story = await AiHelper.TellStoryAsync(storyPrompt, cts.Token) ?? "";
WriteLine(story);
WriteLine();

// Convert the story to audio
AiHelper.WriteTitle("Convert the story to audio...");

await AiHelper.CreateAudioFromStringAsync(story, cts.Token);
