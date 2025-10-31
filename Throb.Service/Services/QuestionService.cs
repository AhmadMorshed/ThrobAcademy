using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace Throb.Service.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public QuestionService(IQuestionRepository repository, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> GenerateAndStoreQuestionsAsync(string transcript, string type)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return "❌ النص المرسل فارغ.";

            var questionsText = await GenerateQuestionsFromTextAsync(transcript, type);
            var questionsLines = questionsText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(l => l.Trim())
                                            .ToArray();

            Question currentQuestion = null;
            List<QuestionOption> currentOptions = new List<QuestionOption>();
            string correctAnswer = null;

            foreach (var line in questionsLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // بدء سؤال جديد (اختيار من متعدد أو صح/خطأ)
                if ((type == "truefalse" || type == "mcq") && (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") || line.StartsWith("4.") || line.StartsWith("5.")))
                {
                    if (currentQuestion != null)
                    {
                        currentQuestion.Options = currentOptions;
                        currentQuestion.CorrectAnswer = correctAnswer ?? ExtractDefaultAnswer(currentQuestion.QuestionText, type);
                        currentQuestion.Difficulty = AssignDifficulty(currentQuestion.QuestionText);
                        await _repository.AddAsync(currentQuestion);
                        Console.WriteLine($"Stored Question: {currentQuestion.QuestionText}, CorrectAnswer: {currentQuestion.CorrectAnswer}");
                        currentOptions = new List<QuestionOption>();
                    }
                    currentQuestion = new Question
                    {
                        Transcript = transcript,
                        QuestionText = line,
                        QuestionType = type,
                        CreatedAt = DateTime.UtcNow
                    };
                    // استخراج الإجابة إذا كانت موجودة في نفس السطر
                    correctAnswer = ExtractAnswer(line) ?? ExtractTrueFalseAnswer(line);
                }
                else if (currentQuestion != null && !string.IsNullOrWhiteSpace(line))
                {
                    // استخراج الإجابة إذا كانت في سطر منفصل
                    if (line.ToLower().Contains("الإجابة الصحيحة") || line.ToLower().Contains("correct answer"))
                    {
                        correctAnswer = ExtractAnswer(line);
                        Console.WriteLine($"Extracted Correct Answer: {correctAnswer}");
                    }
                    // إضافة الخيارات لأسئلة الاختيار من متعدد
                    else if (type == "mcq" && !line.StartsWith("1.") && !line.StartsWith("2.") && !line.StartsWith("3.") && !line.StartsWith("4.") && !line.StartsWith("5."))
                    {
                        currentOptions.Add(new QuestionOption { OptionText = line });
                    }
                }
            }

            // حفظ السؤال الأخير
            if (currentQuestion != null)
            {
                currentQuestion.Options = currentOptions;
                currentQuestion.CorrectAnswer = correctAnswer ?? ExtractDefaultAnswer(currentQuestion.QuestionText, type);
                currentQuestion.Difficulty = AssignDifficulty(currentQuestion.QuestionText);
                await _repository.AddAsync(currentQuestion);
                Console.WriteLine($"Stored Question: {currentQuestion.QuestionText}, CorrectAnswer: {currentQuestion.CorrectAnswer}");
            }

            return questionsText;
        }

        public async Task<List<Question>> GetAllQuestionsAsync()
        {
            return await _repository.GetAllAsync();
        }

        private async Task<string> GenerateQuestionsFromTextAsync(string transcript, string type)
        {
            var openRouterApiKey = _configuration["OpenRouterApiKey"] ?? "";
            if (string.IsNullOrEmpty(openRouterApiKey))
                return "❌ مفتاح OpenRouter غير مهيأ.";

            string language = DetectLanguage(transcript);
            string prompt = BuildPrompt(transcript, type, language);

            var requestBody = new
            {
                model = "meta-llama/llama-3.3-70b-instruct",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.7,
                max_tokens = 1024
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openRouterApiKey);

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);

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

        private string DetectLanguage(string text)
        {
            return text.Any(c => c >= 0x0600 && c <= 0x06FF) ? "ar" : "en";
        }

        private string BuildPrompt(string transcript, string type, string language)
        {
            return language == "ar"
                ? type switch
                {
                    "truefalse" => $@"أنت مساعد ذكي. استخرج من النص التالي مجموعة مناسبة من أسئلة الامتحان من نوع صح أو خطأ، حسب محتوى النص:
النص: {transcript}
الرجاء: 
- توليد 5 أسئلة صح أو خطأ على الأقل.
- قم بكتابة كل سؤال على سطر جديد مع ترقيم (مثال: 1. هل السماء زرقاء؟).
- حدد الإجابة الصحيحة مباشرة بعد كل سؤال في نفس السطر بصيغة: (الإجابة: صح) أو (الإجابة: خطأ).
مثال التنسيق:
1. هل الماء يغلي عند 100 درجة مئوية؟ (الإجابة: صح)
2. هل الشمس تدور حول الأرض؟ (الإجابة: خطأ)",
                    _ => $@"أنت أداة توليد أسئلة امتحانية. قم بإنشاء **7 أسئلة اختيار من متعدد** حول النص التالي.
لكل سؤال: - 4 خيارات (أ، ب، ج، د). - حدد الإجابة الصحيحة بصيغة: (الإجابة الصحيحة: ب)
النص: {transcript}
مثال التنسيق:
1. ما لون السماء؟ أ) أحمر ب) أزرق ج) أخضر د) أصفر (الإجابة الصحيحة: ب)"
                }
                : type switch
                {
                    "truefalse" => $@"You are a helpful assistant. Based on the following text, generate a relevant set of **True or False exam questions**:
Text: {transcript}
Instructions:
- Generate at least 5 True/False questions.
- Write each question on a new line with numbering (e.g., 1. Is the sky blue?).
- Specify the correct answer immediately after each question in the format: (Correct answer: True) or (Correct answer: False).
Example format:
1. Is water boiling at 100 degrees Celsius? (Correct answer: True)
2. Does the Sun revolve around the Earth? (Correct answer: False)",
                    _ => $@"You are an exam question generator. Create **7 multiple choice questions (MCQ)** about the following content.
Each question should: - Include 4 options labeled (A), (B), (C), and (D). - End with the correct answer in the format: (Correct answer: B)
Text: {transcript}
Example format:
1. What is the color of the sky? A) Red B) Blue C) Green D) Yellow (Correct answer: B)"
                };
        }

        private string ExtractAnswer(string line)
        {
            var answerMatch = System.Text.RegularExpressions.Regex.Match(line, @"(الإجابة الصحيحة|Correct answer):\s*(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return answerMatch.Success ? answerMatch.Groups[2].Value.Trim() : null;
        }

        private string ExtractTrueFalseAnswer(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\((الإجابة|Correct answer):\s*(صح|خطأ|True|False)\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[2].Value.Trim() : null; // استرجاع النص فقط (صح/خطأ أو True/False)
        }

        private string ExtractDefaultAnswer(string questionText, string type)
        {
            // تحسين المنطق الافتراضي
            if (type == "truefalse" && questionText.ToLower().Contains("هل"))
                return "خطأ"; // افتراضي باللغة العربية إذا لم يتم تحديد الإجابة
            return null; // لأسئلة MCQ، نتركها فارغة إذا لم يتم العثور على إجابة
        }

        private string AssignDifficulty(string questionText)
        {
            // منطق بسيط لتعيين الصعوبة بناءً على طول النص
            if (questionText.Length < 30) return "Easy";
            else if (questionText.Length < 60) return "Medium";
            else return "Hard";
        }
    }
}