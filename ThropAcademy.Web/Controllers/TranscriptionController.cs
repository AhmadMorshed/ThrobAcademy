using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TranscriptionController : Controller
{
    private readonly string openRouterApiKey;
    private readonly string deepgramApiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    public TranscriptionController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        openRouterApiKey = configuration["OpenRouterApiKey"] ?? "";
        deepgramApiKey = configuration["DeepgramApiKey"] ?? "";
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult UploadMedia() => View();

    [HttpPost]
    public async Task<IActionResult> UploadMedia(IFormFile mediaFile, string method = "local")
    {
        if (mediaFile == null || mediaFile.Length == 0)
            return BadRequest("لم يتم رفع الملف بشكل صحيح.");

        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);

        var extension = Path.GetExtension(mediaFile.FileName);
        var supportedExtensions = new[] { ".mp3", ".wav", ".mp4", ".m4a", ".webm" };
        if (!supportedExtensions.Contains(extension.ToLower()))
            return BadRequest("نوع الملف غير مدعوم.");

        var uniqueFileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(uploads, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await mediaFile.CopyToAsync(stream);
        }

        if (!System.IO.File.Exists(filePath))
            return BadRequest("لم يتم حفظ الملف بنجاح.");

        string transcript;

        try
        {
            if (method == "api")
            {
                transcript = await TranscribeLongVideoAsync(filePath);
            }
            else
            {
                transcript = RunWhisper(filePath);
            }
        }
        catch (Exception ex)
        {
            return BadRequest("حدث خطأ أثناء تحويل الملف: " + ex.Message);
        }
        finally
        {
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        ViewBag.Transcription = transcript;
        return View("Result");
    }
    [HttpPost]
    public async Task<IActionResult> GenerateQuestions(string text, string type)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ViewBag.Questions = "❌ النص المرسل فارغ.";
            return View("Result");
        }

        try
        {
            var questions = await GenerateQuestionsFromTextAsync(text, type);
            ViewBag.Transcription = text;
            ViewBag.Questions = questions;
        }
        catch (Exception ex)
        {
            ViewBag.Questions = "❌ حدث خطأ أثناء توليد الأسئلة: " + ex.Message;
        }

        return View("Result");
    }

    private List<string> SplitVideoIntoChunks(string videoPath, int chunkDurationSeconds = 60)
    {
        var outputFiles = new List<string>();
        var outputDir = Path.Combine(Path.GetDirectoryName(videoPath), "chunks");
        Directory.CreateDirectory(outputDir);

        // استخدم الامتداد المناسب لملف الإدخال، لتجنب مشاكل codec
        var extension = Path.GetExtension(videoPath).ToLower();
        var outputExtension = extension == ".webm" ? ".webm" : ".mp4";

        var args = $"-i \"{videoPath}\" -c copy -map 0 -f segment -segment_time {chunkDurationSeconds} \"{Path.Combine(outputDir, $"chunk_%03d{outputExtension}")}\"";

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi))
        {
            process.WaitForExit();
        }

        outputFiles.AddRange(Directory.GetFiles(outputDir, $"chunk_*{outputExtension}"));
        return outputFiles;
    }


    private async Task<string> TranscribeLongVideoAsync(string videoPath)
    {
        var chunks = SplitVideoIntoChunks(videoPath, 60);
        if (chunks.Count == 0)
            return "❌ لم يتم تقسيم الفيديو إلى أجزاء.";

        var fullTranscript = new StringBuilder();

        foreach (var chunkVideo in chunks)
        {
            try
            {
                var audioPath = ConvertVideoToAudio(chunkVideo);
                if (audioPath == null)
                {
                    fullTranscript.AppendLine($"❌ فشل تحويل جزء الفيديو إلى صوت: {Path.GetFileName(chunkVideo)}");
                    continue;
                }

                var text = await TranscribeWithDeepgramAsync(audioPath);
                if (!string.IsNullOrWhiteSpace(text))
                    fullTranscript.AppendLine(text);

                if (System.IO.File.Exists(audioPath))
                    System.IO.File.Delete(audioPath);

                if (System.IO.File.Exists(chunkVideo))
                    System.IO.File.Delete(chunkVideo);
            }
            catch (Exception ex)
            {
                fullTranscript.AppendLine($"❌ خطأ أثناء تفريغ جزء: {Path.GetFileName(chunkVideo)} - {ex.Message}");
            }
        }

        var chunksDir = Path.Combine(Path.GetDirectoryName(videoPath), "chunks");
        if (Directory.Exists(chunksDir) && Directory.GetFiles(chunksDir).Length == 0)
            Directory.Delete(chunksDir);

        return fullTranscript.ToString();
    }

    private string RunWhisper(string filePath)
    {
        var scriptPath = @"C:\\Users\\Ahmad Nakawa\\Desktop\\Throb\\ThropAcademy.Web\\PythonScripts\\whisper_transcribe.py";

        if (!System.IO.File.Exists(scriptPath))
            throw new FileNotFoundException("ملف سكربت بايثون غير موجود: " + scriptPath);

        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("ملف الصوت غير موجود: " + filePath);

        var psi = new ProcessStartInfo
        {
            FileName = @"C:\\Users\\Ahmad Nakawa\\AppData\\Local\\Programs\\Python\\Python312\\python.exe",
            Arguments = $"\"{scriptPath}\" \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (var process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string errors = process.StandardError.ReadToEnd();
            process.WaitForExit();

            System.IO.File.WriteAllText("whisper_output.log", output, Encoding.UTF8);
            System.IO.File.WriteAllText("whisper_error.log", errors, Encoding.UTF8);

            if (process.ExitCode != 0)
                throw new Exception("خطأ في تنفيذ سكربت Whisper: " + errors);

            return string.IsNullOrWhiteSpace(output) ? "❌ لم يتم استخراج نص." : output;
        }
    }

    private string ConvertVideoToAudio(string videoPath)
    {
        var audioPath = Path.ChangeExtension(videoPath, ".wav");

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{videoPath}\" -vn -ac 1 -ar 16000 -f wav \"{audioPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new Exception("خطأ أثناء تحويل الفيديو إلى صوت: " + error);
            }
        }

        return System.IO.File.Exists(audioPath) ? audioPath : null;
    }

    private async Task<string> TranscribeWithDeepgramAsync(string filePath)
    {
        if (string.IsNullOrEmpty(deepgramApiKey))
            return "❌ مفتاح Deepgram غير مهيأ.";

        var client = _httpClientFactory.CreateClient("Deepgram");
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Add("Authorization", $"Token {deepgramApiKey}");

        await using var fileStream = System.IO.File.OpenRead(filePath);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        var response = await client.PostAsync("https://api.deepgram.com/v1/listen", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return $"❌ خطأ من Deepgram: {response.StatusCode} - {error}";
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var transcript = doc.RootElement
                            .GetProperty("results")
                            .GetProperty("channels")[0]
                            .GetProperty("alternatives")[0]
                            .GetProperty("transcript")
                            .GetString();

        return string.IsNullOrWhiteSpace(transcript) ? "❌ لم يتم استخراج أي نص من الملف." : transcript;
    }



    private async Task<string> GenerateQuestionsFromTextAsync(string transcript, string type)
    {
        if (string.IsNullOrEmpty(openRouterApiKey))
            return "❌ مفتاح OpenRouter غير مهيأ.";

        string language = DetectLanguage(transcript);

        string prompt = "";

        if (language == "ar")
        {
            prompt = type switch
            {
                "truefalse" => $@"أنت مساعد ذكي. استخرج من النص التالي مجموعة مناسبة من أسئلة الامتحان من نوع صح أو خطأ، حسب محتوى النص:

النص:
{transcript}

الرجاء:
- توليد عدد كافٍ من أسئلة صح أو خطأ.
- بيان الإجابة الصحيحة مع كل سؤال بصيغة: (الإجابة: صح) أو (الإجابة: خطأ).",
                _ => $@"أنت أداة توليد أسئلة امتحانية.
قم بإنشاء **7 أسئلة اختيار من متعدد** حول النص التالي.

لكل سؤال:
- 4 خيارات (أ، ب، ج، د).
- حدد الإجابة الصحيحة بصيغة: (الإجابة الصحيحة: ب)

النص:
{transcript}"
            };
        }
        else // English
        {
            prompt = type switch
            {
                "truefalse" => $@"You are a helpful assistant. Based on the following text, generate a relevant set of **True or False exam questions**:

Text:
{transcript}

Instructions:
- Create multiple True/False questions.
- Include the correct answer with each question in the format: (Correct answer: True) or (Correct answer: False)",
                _ => $@"You are an exam question generator.
Create **7 multiple choice questions (MCQ)** about the following content.

Each question should:
- Include 4 options labeled (A), (B), (C), and (D).
- End with the correct answer in the format: (Correct answer: B)

Text:
{transcript}"
            };
        }

        // Log prompt for debugging
        System.Diagnostics.Debug.WriteLine("🟦 PROMPT SENT TO OPENROUTER:\n" + prompt);

        var requestBody = new
        {
            model = "meta-llama/llama-3.3-70b-instruct",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
            max_tokens = 1024
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openRouterApiKey);

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return $"❌ خطأ في API: {response.StatusCode} - {error}";
        }

        var responseJson = await response.Content.ReadAsStringAsync();

        // Log response for debugging
        System.Diagnostics.Debug.WriteLine("🟩 OPENROUTER RAW RESPONSE:\n" + responseJson);

        using var doc = JsonDocument.Parse(responseJson);
        var answer = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

        return answer ?? "❌ لم يتم توليد أسئلة.";
    }

    private string DetectLanguage(string text)
    {
        return text.Any(c => c >= 0x0600 && c <= 0x06FF) ? "ar" : "en";

    }
}