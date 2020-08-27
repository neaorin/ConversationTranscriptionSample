# Real-time Conversation Transcription using Microsoft Cognitive Services

This is sample C# code for using the [Cognitive Services Speeck SDK](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-sdk) to [transcribe meetings](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-use-conversation-transcription) and other conversations with the ability to add, remove, and identify multiple participants.

This sample uses .NET Core.

## Requirements

1. [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core) or later
2. [An Azure subscription](https://azure.microsoft.com/en-us/free/cognitive-services/)
3. [A speech service](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/) created in your Azure subscription. 
    > NOTE: Right now, conversation transcription features are only available in `centralus` and `eastasia` regions.
3. (optional) [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/) with the [vscode-solution-explorer](https://github.com/fernandoescolar/vscode-solution-explorer/) extension

## How to run the sample

Open `Properties\launchSettings.json` and replace the speech service region and key with the values from your Azure Speech service:

```json
    "ConversationTranscriptionSample": {
      "commandName": "Project",
      "environmentVariables": {
        "CognitiveServicesSpeechRegion": "<your service region>",
        "CognitiveServicesSpeechKey": "<your key>"
      }
```

In VS or VSCode, build and run the `ConversationTranscriptionSample` project.

You can also do this using the [`dotnet` driver](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet):

```
dotnet restore
dotnet build .\ConversationTranscriptionSample.csproj
dotnet run --project .\ConversationTranscriptionSample.csproj
```