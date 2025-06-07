using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TranscriptionController : Controller
{
    private readonly string openRouterApiKey = "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";

    [HttpGet]
    public IActionResult UploadMedia()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadMedia(IFormFile mediaFile)
    {
        if (mediaFile != null && mediaFile.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);

            var extension = Path.GetExtension(mediaFile.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploads, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await mediaFile.CopyToAsync(stream);
            }

            if (!System.IO.File.Exists(filePath))
                return BadRequest("لم يتم حفظ الملف الصوتي بنجاح.");

            string transcript = RunWhisper(filePath);

            ViewBag.Transcription = transcript;
            return View("Result");
        }

        return BadRequest("لم يتم رفع الملف بشكل صحيح.");
    }

    private string RunWhisper(string filePath)
    {
        var scriptPath = @"C:\Users\Ahmad Nakawa\Desktop\Throb\ThropAcademy.Web\PythonScripts\whisper_transcribe.py";

        if (!System.IO.File.Exists(scriptPath))
            throw new FileNotFoundException("ملف سكربت بايثون غير موجود: " + scriptPath);

        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("ملف الصوت غير موجود: " + filePath);

        var psi = new ProcessStartInfo
        {
            FileName = @"C:\Users\Ahmad Nakawa\AppData\Local\Programs\Python\Python312\python.exe",
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

            // سجل الاخطاء والنواتج لو احتجت
            System.IO.File.WriteAllText("whisper_output.log", output);
            System.IO.File.WriteAllText("whisper_error.log", errors);

            return string.IsNullOrWhiteSpace(output) ? "❌ لم يتم استخراج نص." : output;
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateQuestions(string text, string type)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest("النص غير موجود.");

        string questions = await GenerateQuestionsFromTextAsync(text, type);

        ViewBag.Transcription = text;
        ViewBag.Questions = questions;
        return View("Result");
    }

    private async Task<string> GenerateQuestionsFromTextAsync(string transcript, string type)
    {
        var prompt = type switch
        {
            "truefalse" => $@"
أنت مساعد ذكي. استخرج من النص التالي مجموعة من أسئلة الامتحان:

النص:
{transcript}

الرجاء توليد:
- 5 أسئلة صح أو خطأ مع بيان الإجابة الصحيحة.
",
            _ => $@"
أنت مساعد ذكي. استخرج من النص التالي مجموعة من أسئلة الامتحان:

النص:
{transcript}

الرجاء توليد:
- 3 أسئلة اختيار من متعدد مع 4 خيارات، وضع الإجابة الصحيحة.
"
        };

        var requestBody = new
        {
            model = "meta-llama/llama-3.3-70b-instruct", // استخدم الموديل المناسب من OpenRouter
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 512
        };

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openRouterApiKey);

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return $"❌ خطأ في API: {response.StatusCode} - {error}";
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var answer = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return answer ?? "❌ لم يتم توليد أسئلة.";
    }
}
