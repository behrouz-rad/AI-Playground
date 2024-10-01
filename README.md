# AI Playground
This repository contains a C# console application that demonstrates the use of various AI models for multiple tasks, including code review, image recognition, and storytelling with audio synthesis. The application leverages AI models hosted by Ollama and Hugging Face to perform these tasks.

## Features
### 1. AI-Powered Code Review
The application utilizes AI models from Ollama to review code changes. For demonstration purposes, the code differences are provided via a fixed file containing the output of a `git diff`. This diff is analyzed by various AI models, offering diverse insights into the code changes.

- **AI Models**: The list of AI models used is defined in ./assets/ai_model.txt. Feel free to add/remove them. Before running the app, these models should be downloaded using:
```bash
ollama pull [model_name]
```

- For a complete list of available AI models, visit [Ollama's library](https://ollama.com/library?sort=popular).
- **Sample Output**: The results from different AI model reviews are stored in the `sample_results` folder, showcasing their ability to provide detailed feedback on code changes.

### 2. Image Recognition with AI
The application uses Ollama’s `LLaVA` model - *please `pull` it* - for image recognition tasks, enabling the following capabilities:
- **Image Description**: Analyze an image and generate a textual description.
- **Text Extraction**: Extract any text found in an image.

### 3. Storytelling and Audio Synthesis
Leveraging Hugging Face and Ollama, the app can take an image, generate a story based on its description, and then convert that story into audio.

1. The image is described using both an Ollama model and Hugging Face model.
2. A story is created based on the description and using an Ollama model (`orca-mini`) - *Please `pull` it*.
3. Hugging Face's API is used to convert the story into an audio file.

**API Key**: To use Hugging Face for text-to-speech conversion, an API key is required. The API key should be stored in `./assets/hg_api_key.txt` and can be generated from [Hugging Face settings](https://huggingface.co/settings/tokens).