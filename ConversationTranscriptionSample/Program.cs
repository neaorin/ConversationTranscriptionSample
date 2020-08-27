using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;

namespace ConversationTranscriptionSample
{

    public class MyConversationTranscriber
    {
        static async Task Main(string[] args)
        {
            var region = Environment.GetEnvironmentVariable("CognitiveServicesSpeechRegion"); 
            var key = Environment.GetEnvironmentVariable("CognitiveServicesSpeechKey"); 

            // Enroll voice signatures
            //var KatieSignature = await CreateVoiceSignatureByUsingBody(@"..\..\enrollment_audio_katie.wav", region, key);
            //var SteveSignature = await CreateVoiceSignatureByUsingBody(@"..\..\enrollment_audio_steve.wav", region, key);

            // live conversation transcription
            await ConversationWithPullAudioStreamAsync("katiesteve.wav", region, key);
        }

        static async Task CreateVoiceSignatureByUsingBody(string filePath, string region, string key)
        {
            // Replace with your own region
            // Change the name of the wave file to match yours
            byte[] fileBytes = File.ReadAllBytes(filePath);
            var content = new ByteArrayContent(fileBytes);

            var client = new HttpClient();
            // Add your subscription key to the header Ocp-Apim-Subscription-Key directly
            // Replace "YourSubscriptionKey" with your own subscription key
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            // Edit with your desired region for `{region}`
            var response = await client.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);
            // A voice signature contains Version, Tag and Data key values from the Signature json structure from the Response body.
            // Voice signature format example: { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }
            var jsonData = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonData);
        }


        public static async Task ConversationWithPullAudioStreamAsync(string filePath, string region, string key)
        {
            // Creates an instance of a speech config with specified subscription key and service region
            // Replace with your own subscription key and region
            var config = SpeechConfig.FromSubscription(key, region);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            var stopTranscription = new TaskCompletionSource<int>();

            var callback = Helper.CreateWavReader(filePath);
            var pullStream = AudioInputStream.CreatePullStream(callback, AudioStreamFormat.GetWaveFormatPCM(16000, 16, 8));

            // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
            // Replace with your own audio file name and Helper class which implements AudioConfig using PullAudioInputStreamCallback
            using (var audioInput = AudioConfig.FromStreamInput(pullStream))
            {
                var meetingId = Guid.NewGuid().ToString();
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingId).ConfigureAwait(false))
                {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Subscribe to events
                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            //Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                if (!String.IsNullOrWhiteSpace(e.Result.Text))
                                    Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text}, UserID={e.Result.UserId}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                stopTranscription.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine("\nSession started event.");
                        };

                        conversationTranscriber.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopTranscription.TrySetResult(0);
                        };

                        // Add participants to the conversation.
                        // Create voice signatures using REST API described in the earlier section in this document.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }

                        /*
                        var speakerA = Participant.From("Katie", "en-us", KatieSignature);
                        var speakerB = Participant.From("Steve", "en-us", SteveSignature);
                        await conversation.AddParticipantAsync(speakerA);
                        await conversation.AddParticipantAsync(speakerB);
                        */

                        // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopTranscription.Task });

                        // Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }
        }


        /*
        public static async Task ConversationWithPullAudioStreamAsyncOld(string filePath, string region, string key)
        {
            // Creates an instance of a speech config with specified subscription key and service region
            // Replace with your own subscription key and region
            var config = SpeechConfig.FromSubscription(key, region);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            var stopTranscription = new TaskCompletionSource<int>();

            // Create an audio stream from a wav file or from the default microphone if you want to stream live audio from the supported devices
            // Replace with your own audio file name and Helper class which implements AudioConfig using PullAudioInputStreamCallback
            using (var audioInput = Helper.OpenWavFile(filePath))
            {
                var meetingId = Guid.NewGuid().ToString();
                using (var conversation = await Conversation.CreateConversationAsync(config, meetingId).ConfigureAwait(false))
                {
                    // Create a conversation transcriber using audio stream input
                    using (var conversationTranscriber = new ConversationTranscriber(audioInput))
                    {
                        await conversationTranscriber.JoinConversationAsync(conversation);

                        // Subscribe to events
                        conversationTranscriber.Transcribing += (s, e) =>
                        {
                            //Console.WriteLine($"TRANSCRIBING: Text={e.Result.Text}");
                        };

                        conversationTranscriber.Transcribed += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                if (!String.IsNullOrWhiteSpace(e.Result.Text))
                                    Console.WriteLine($"TRANSCRIBED: Text={e.Result.Text}, UserID={e.Result.UserId}");
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        conversationTranscriber.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                stopTranscription.TrySetResult(0);
                            }
                        };

                        conversationTranscriber.SessionStarted += (s, e) =>
                        {
                            Console.WriteLine("\nSession started event.");
                        };

                        conversationTranscriber.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine("\nSession stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopTranscription.TrySetResult(0);
                        };

                        // Add participants to the conversation.
                        // Create voice signatures using REST API described in the earlier section in this document.
                        // Voice signature needs to be in the following format:
                        // { "Version": <Numeric string or integer value>, "Tag": "string", "Data": "string" }


                        var speakerA = Participant.From("Katie", "en-us", KatieSignature);
                        var speakerB = Participant.From("Steve", "en-us", SteveSignature);
                        await conversation.AddParticipantAsync(speakerA);
                        await conversation.AddParticipantAsync(speakerB);


                        // Starts transcribing of the conversation. Uses StopTranscribingAsync() to stop transcribing when all participants leave.
                        await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopTranscription.Task });

                        // Stop transcribing the conversation.
                        await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        */

        static readonly string KatieSignature = @"{""Status"":""OK"",""Signature"":{""Version"":""0"",""Tag"":""OjW97xENsR4UMppo1nk3YZeW2NH8ElNF4NJ1Yf1xOBc="",""Data"":""U07rQdur4ZgIzacnDSSMCicch251/T818G2FQJa+MV49f2vtEOXZFFDb1p1hNPmZzlpc7i9wwSGge4BqTMKFJFWQOPD02A6g7L8xabzJJycGE9JER0Hz6Rhv6x8A/zODbRFHxPbvKeawY+JHHSDYlhnRJDdJ7qU+ThNdtmyUHt/pmJAijM2Fj+jP2idQ5OGlz/mYH4hispGjGGakQ6iCsPP+ppVPSAWuxN7u2/26T3F0F9jnK8Jheim1DfHkIDHE5xnm710xnxw+HhYnDcBAseRuJUMDhwpRdF7VCpCIhnj27lgALCNd73W7SIDLwu5f5N3CSCZ/4vJmKPec9EFJzznWmn3WVUlh6V+MTZEJddK7PvPFhBbrsWimM+2oGjJV2s7X/SINfUvpm5+dlogUzp2l0ESl6lEiRQXN9/T327CHUXsKEPAC8SqnZfBOL2JA2Omn5i9F49taf5b6YdbW6fHr/uDYATAXkX5SjxUKBbc4Jdpp3uBzUJAHB+tTW9xC687K1EQHmLxPKipJ1aUeXZjvXfIhM/BS18RtQw8x8sG2Pb93GOIha8pq9cQhoRJW00lkLNUwHxwhtzmm4AxOO0ulcXxgEkTgeD2eNPyhfGQ6O4Kom7I1uTAiaCp7hzJDM60pex1sbcjxAdi80TaZEMS6TZ2cDDqZfeQY0wqP6z8vZX3uA4X/0PTjtkybGLYR""},""Transcription"":null}";
        static readonly string SteveSignature = @"{""Status"":""OK"", ""Signature"":{ ""Version"":""0"", ""Tag"":""yN8E09QhI7/b9mH/oCwzOO9C5M+JgZSgq5kjmRSKUoc="", ""Data"":""tf5HL8Hl5Vs1L/vstdYo8M0eJ9aHHu6zulktip33YCd66bcVfiEBfPOz8+1h935I8viFY82kvNHQLbeMrzlFydbbfVYhM70VAPgOX8tFRYI6SSnzfFCgEWGbjNeGE7ASMdeSfAxj7kablcAOI90oba6N1E7du/7R1HF7BiaQspXsVYXDIZsAHS3ipffitqh4qEKnYJl+0l1RjAiWOQo0sKStss2ahqqgn3c4keb/0vR0KOtEdaRexoWDUjWU/MT2JYTckFLYQ4vTaU7aEy3HyLdbMEB60d6i20iWvOaKrDkb5tS+rqvmsORBCsB5o7i7eS98czPdOk902jPqxPJjQRg6XkH3FBmoqKxKNtWP0n+UUGXzXZpFHTWQRHo2xH4QF3G/JX2icysySs72n6fQSN02T1IkVnAG6qZNoPz5/eGWDcWjh7NV64tdVwFI2fy98t9CXuZrdoO0wqz75jRE90RMxQc8SE/jTbKq2ai/uryl+HZJjY5EZfNnjf1F69cIMos7gB2Lq+79sgG7/TqaWlsHfwev9608IMXh+TESQRzCz0fH3hLwzgmp4Jjlsxvqaa9RQNvth9eVn4DeSfka5ssVH8JnjI/WueE+OGCmhoagtlb/NyFXf536sQplk4JUygZNw3NIL7TgQpcX22Nw/IOoQGyRvf0wNGs1P390cAgoBMJIT1Too5vpmNSgMrTV"" }, ""Transcription"":null }";

    }
}
