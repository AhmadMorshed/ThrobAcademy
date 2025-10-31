using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Throb.Data.Entities;
using Throb.Service.Interfaces;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace Throb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TranscriptionController : Controller
    {
        private readonly string deepgramApiKey;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IQuestionService _questionService;
        private readonly IExamRequestService _examRequestService;

        public TranscriptionController(IConfiguration configuration, IHttpClientFactory httpClientFactory,
            IQuestionService questionService, IExamRequestService examRequestService)
        {
            deepgramApiKey = configuration["DeepgramApiKey"] ?? "";
            _httpClientFactory = httpClientFactory;
            _questionService = questionService;
            _examRequestService = examRequestService;
        }

        // ----------------------------------------------------------------------
        // دوال المساعدة للمقارنة (المضافة والمُصححة)
        // ----------------------------------------------------------------------

        // دالة مساعدة لتنظيف السلسلة النصية (تزيل الفراغات الزائدة والأسطر الجديدة)
        private string NormalizeAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return string.Empty;

            string result = answer.Trim();

            // استبدال جميع أحرف الأسطر الجديدة بفراغ
            result = result.Replace("\r\n", " ").Replace("\n", " ");

            // إزالة أي فراغات متعددة متتالية وترك فراغ واحد فقط
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        // دالة جديدة لاستخلاص رمز الخيار (مثل "أ)") من بداية النص
        private string ExtractPrefixFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // النمط يطابق: حرف عربي/رقم (1) متبوع بـ . أو ) أو قوس، في بداية السلسلة
            var match = Regex.Match(text, @"^\s*([\w\u0621-\u064A]{1}[\.\)])", RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                // إرجاع الرمز كاملاً (مثلاً "أ)")
                return match.Groups[1].Value.Trim();
            }

            // إذا لم يتم العثور على رمز، نرجع النص كاملاً بعد التنظيف الأساسي
            return text.Trim();
        }

        // ----------------------------------------------------------------------
        // دوال الإجراءات (Actions)
        // ----------------------------------------------------------------------

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
                var questionsText = await _questionService.GenerateAndStoreQuestionsAsync(text, type);
                ViewBag.Transcription = text;
                ViewBag.Questions = questionsText;
            }
            catch (Exception ex)
            {
                ViewBag.Questions = "❌ حدث خطأ أثناء توليد الأسئلة: " + ex.Message;
            }

            return View("Result");
        }

        [HttpGet]
        public async Task<IActionResult> CreateExam()
        {
            var questions = await _questionService.GetAllQuestionsAsync();
            ViewBag.Questions = questions;
            return View(new ExamRequestModel { ExamRequestId = 0 }); // أو قيمة افتراضية
        }

        [HttpPost]
        public async Task<IActionResult> CreateExam(int id, ExamRequestModel model, int[] SelectedQuestionIds)
        {
            Console.WriteLine($"Received id: {id}, Model: {(model != null ? "not null" : "null")}");
            if (model == null)
            {
                Console.WriteLine("Model is null. Request Form data: " + string.Join(", ", Request.Form.Keys));
                ModelState.AddModelError("", "لم يتم استلام نموذج الامتحان بشكل صحيح. تحقق من الحقول.");
                var questions = await _questionService.GetAllQuestionsAsync();
                ViewBag.Questions = questions;
                return View(new ExamRequestModel { ExamRequestId = 0 });
            }

            // Diagnostic: log detailed ModelState errors (keys + attempted values + messages)
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is INVALID. Dumping entries:");
                foreach (var kv in ModelState)
                {
                    var key = string.IsNullOrEmpty(kv.Key) ? "<root>" : kv.Key;
                    var raw = kv.Value?.RawValue;
                    var rawStr = raw == null ? "(null)" : (raw is string ? raw.ToString() : $"[{string.Join(",", (raw as Array ?? Array.Empty<object>()).Cast<object>())}]");
                    var errors = kv.Value.Errors.Select(e => e.ErrorMessage + (e.Exception != null ? " | " + e.Exception.Message : "")).ToList();
                    Console.WriteLine($"  Key: {key} | Raw: {rawStr} | Errors: {string.Join(" ; ", errors)}");
                }

                var questions = await _questionService.GetAllQuestionsAsync();
                ViewBag.Questions = questions;
                return View(model);
            }

            try
            {
                var examRequest = id == 0 ? model : await _examRequestService.GetByIdAsync(id);

                if (examRequest == null)
                {
                    Console.WriteLine($"examRequest is null. id: {id}, model: {(model != null ? "not null" : "null")}");
                    examRequest = model ?? new ExamRequestModel { ExamRequestId = id };
                    if (id != 0)
                    {
                        ModelState.AddModelError("", "لم يتم العثور على الامتحان المطلوب.");
                        var questions = await _questionService.GetAllQuestionsAsync();
                        ViewBag.Questions = questions;
                        return View(model);
                    }
                }

                examRequest.NumberOfQuestions = model.NumberOfQuestions;
                examRequest.IncludeMCQ = model.IncludeMCQ;
                examRequest.IncludeTrueFalse = model.IncludeTrueFalse;
                examRequest.EasyCount = model.EasyCount;
                examRequest.MediumCount = model.MediumCount;
                examRequest.HardCount = model.HardCount;

                if (examRequest.Questions == null) examRequest.Questions = new List<Question>();

                if (SelectedQuestionIds != null && SelectedQuestionIds.Length > 0)
                {
                    var allQuestions = await _questionService.GetAllQuestionsAsync();
                    var selectedQuestions = allQuestions.Where(q => SelectedQuestionIds.Contains(q.QuestionId)).ToList();
                    examRequest.Questions.AddRange(selectedQuestions);
                    Console.WriteLine($"Selected {selectedQuestions.Count} questions from bank.");
                }

                if (!examRequest.Questions.Any())
                {
                    var allQuestions = await _questionService.GetAllQuestionsAsync();
                    if (!allQuestions.Any())
                    {
                        ModelState.AddModelError("", "لا يوجد أسئلة في البنك لإنشاء الامتحان.");
                        ViewBag.Questions = allQuestions;
                        return View(model);
                    }

                    var examQuestions = new List<Question>();
                    if (model.IncludeMCQ)
                    {
                        var mcqQuestions = allQuestions.Where(q => q.QuestionType == "mcq").ToList();
                        examQuestions.AddRange(SelectQuestionsByDifficulty(mcqQuestions, model.EasyCount, model.MediumCount, model.HardCount));
                    }
                    if (model.IncludeTrueFalse)
                    {
                        var trueFalseQuestions = allQuestions.Where(q => q.QuestionType == "truefalse").ToList();
                        examQuestions.AddRange(SelectQuestionsByDifficulty(trueFalseQuestions, model.EasyCount, model.MediumCount, model.HardCount));
                    }
                    if (examQuestions.Count < model.NumberOfQuestions)
                    {
                        ModelState.AddModelError("", $"عدد الأسئلة المتاحة ({examQuestions.Count}) أقل من العدد المطلوب ({model.NumberOfQuestions}).");
                        ViewBag.Questions = allQuestions;
                        return View(model);
                    }
                    examRequest.Questions.AddRange(examQuestions.OrderBy(q => Guid.NewGuid()).Take(model.NumberOfQuestions));
                    Console.WriteLine($"Generated {examQuestions.Count} questions based on difficulty.");
                }

                ExamRequestModel savedExamRequest;
                try
                {
                    savedExamRequest = await _examRequestService.CreateExamRequestAsync(examRequest);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CreateExamRequestAsync threw: {ex.Message}");
                    ModelState.AddModelError("", "حدث خطأ أثناء حفظ الامتحان: " + ex.Message);
                    var questions = await _questionService.GetAllQuestionsAsync();
                    ViewBag.Questions = questions;
                    return View(model);
                }

                if (savedExamRequest == null || savedExamRequest.ExamRequestId == 0)
                {
                    ModelState.AddModelError("", "فشل في حفظ الامتحان. الرجاء المحاولة لاحقًا.");
                    Console.WriteLine("Failed to save exam request. Questions count: " + (examRequest.Questions?.Count ?? 0));
                    var questions = await _questionService.GetAllQuestionsAsync();
                    ViewBag.Questions = questions;
                    return View(model);
                }

                Console.WriteLine($"Exam created with ID: {savedExamRequest.ExamRequestId}");
                TempData["SuccessMessage"] = "تم إنشاء الامتحان بنجاح!";
                return RedirectToAction("ExamPreview", new { id = savedExamRequest.ExamRequestId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الامتحان: " + ex.Message);
                Console.WriteLine($"Exception in CreateExam: {ex.Message} - {ex.StackTrace}");
                var questions = await _questionService.GetAllQuestionsAsync();
                ViewBag.Questions = questions;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddManualQuestionToExam(int examId)
        {
            ViewBag.ExamId = examId;
            return View(new Question());
        }

        [HttpPost]
        public async Task<IActionResult> AddManualQuestionToExam(int examId, [FromForm] Question question)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "البيانات غير صالحة." });
            }

            try
            {
                var examRequest = await _examRequestService.GetByIdAsync(examId) ?? new ExamRequestModel { ExamRequestId = examId };
                question.IsManual = true;
                question.CreatedAt = DateTime.UtcNow;
                examRequest.Questions.Add(question);
                await _examRequestService.UpdateExamRequestAsync(examRequest);

                return Json(new { success = true, message = "تم حفظ السؤال بنجاح!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }


        private List<string> SplitVideoIntoChunks(string videoPath, int chunkDurationSeconds = 60)
        {
            var outputFiles = new List<string>();
            var outputDir = Path.Combine(Path.GetDirectoryName(videoPath), "chunks");
            Directory.CreateDirectory(outputDir);

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

        [HttpGet]
        public async Task<IActionResult> QuestionBank()
        {
            var questions = await _questionService.GetAllQuestionsAsync();
            return View(questions);
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
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

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

        [HttpGet]
        public async Task<IActionResult> EditExam(int id)
        {
            var examRequest = await _examRequestService.GetByIdAsync(id);
            if (examRequest == null)
                return NotFound();

            return View(examRequest);
        }

        [HttpPost]
        public async Task<IActionResult> EditExam(ExamRequestModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _examRequestService.UpdateExamRequestAsync(model);
            return RedirectToAction("QuestionBank");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExam(int id)
        {
            await _examRequestService.DeleteExamRequestAsync(id);
            return RedirectToAction("QuestionBank");
        }

        [HttpGet]
        public IActionResult AddManualQuestion(int examId)
        {
            ViewBag.ExamId = examId;
            return View(new Question());
        }

        [HttpPost]
        public async Task<IActionResult> AddManualQuestion(int examId, Question question)
        {
            if (!ModelState.IsValid)
                return View(question);

            await _examRequestService.AddManualQuestionAsync(examId, question);
            return RedirectToAction("EditExam", new { id = examId });
        }

        private List<Question> SelectQuestionsByDifficulty(List<Question> questions, int easyCount, int mediumCount, int hardCount)
        {
            var easy = questions.Where(q => q.Difficulty == "Easy").OrderBy(q => Guid.NewGuid()).Take(easyCount).ToList();
            var medium = questions.Where(q => q.Difficulty == "Medium").OrderBy(q => Guid.NewGuid()).Take(mediumCount).ToList();
            var hard = questions.Where(q => q.Difficulty == "Hard").OrderBy(q => Guid.NewGuid()).Take(hardCount).ToList();
            return easy.Concat(medium).Concat(hard).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> ExamPreview(int id)
        {
            var examRequest = await _examRequestService.GetByIdAsync(id);
            if (examRequest == null)
                return NotFound();

            return View(examRequest);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitExam(ExamRequestModel model, [FromForm] Dictionary<string, string> answers)
        {
            Console.WriteLine("SubmitExam called. Model: " + (model != null ? "not null" : "null"));
            Console.WriteLine("Received answers count: " + answers.Count);

            if (model == null || model.Questions == null || answers == null)
            {
                Console.WriteLine("Invalid input: Model or Questions or answers is null.");
                return View("ExamPreview", model);
            }

            // جلب الإجابات الصحيحة من قاعدة البيانات
            var questionIds = model.Questions.Select(q => q.QuestionId).ToList();
            var allQuestions = await _questionService.GetAllQuestionsAsync();
            var correctMap = allQuestions
                .Where(q => questionIds.Contains(q.QuestionId))
                .ToDictionary(q => q.QuestionId, q => q.CorrectAnswer ?? string.Empty);

            int score = 0;
            foreach (var question in model.Questions)
            {
                string userAnswer = answers.ContainsKey(question.QuestionId.ToString()) ? answers[question.QuestionId.ToString()] : null;
                correctMap.TryGetValue(question.QuestionId, out var correctAnswer);

                // تنظيف أساسي
                string normalizedUserAnswer = NormalizeAnswer(userAnswer);
                string normalizedCorrectAnswer = NormalizeAnswer(correctAnswer);

                bool isMatch = false;

                if (question.QuestionType.ToLower() == "mcq")
                {
                    // **منطق MCQ:**
                    // إجابة DB هي الرمز (مثل 'أ)')
                    // إجابة المستخدم هي النص الكامل للخيار (مثل 'أ) ملف فيديو')

                    // 1. استخلاص الرمز الذي اختاره المستخدم من النص الكامل (مثلاً: يحول 'أ) ملف فيديو' إلى 'أ)')
                    string userSelectedPrefix = ExtractPrefixFromText(userAnswer); // استخدام userAnswer الأصلي للحصول على الرمز

                    // 2. مقارنة رمز المستخدم المستخلص بـ الإجابة المخزنة في DB (التي هي الرمز)
                    if (string.Equals(userSelectedPrefix, normalizedCorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                    }

                }
                else if (question.QuestionType.ToLower() == "truefalse")
                {
                    // **منطق True/False:**
                    // الإجابة المخزنة هي 'صح' أو 'خطأ' (عربي)
                    // الإجابة المرسلة هي 'True' أو 'False' (إنجليزي)

                    string translatedCorrectAnswer = "";
                    if (normalizedCorrectAnswer.Equals("صح", StringComparison.OrdinalIgnoreCase))
                        translatedCorrectAnswer = "True";
                    else if (normalizedCorrectAnswer.Equals("خطأ", StringComparison.OrdinalIgnoreCase))
                        translatedCorrectAnswer = "False";

                    // المقارنة بالقيم الإنجليزية المرسلة
                    if (!string.IsNullOrWhiteSpace(translatedCorrectAnswer) && string.Equals(normalizedUserAnswer, translatedCorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                    }
                }
                else
                {
                    // أسئلة أخرى (مقالية/نصية) - مقارنة النص بالكامل
                    if (string.Equals(normalizedUserAnswer, normalizedCorrectAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = true;
                    }
                }

                // *** التشخيص والمحاسبة ***
                Console.WriteLine($"Q ID: {question.QuestionId}, Type: {question.QuestionType}, Correct DB: '{correctAnswer}', User Ans: '{userAnswer}'");
                Console.WriteLine($"   Normalized Correct: '{normalizedCorrectAnswer}', User Prefix: '{ExtractPrefixFromText(userAnswer)}'");
                if (isMatch)
                {
                    score++;
                    Console.WriteLine($"--- MATCH FOUND for ID: {question.QuestionId} ---");
                }
                else
                {
                    Console.WriteLine($"--- MATCH FAILED for ID: {question.QuestionId} ---");
                }
            } // نهاية حلقة الأسئلة

            var total = model.Questions.Count;
            ViewBag.Score = score;
            ViewBag.Total = total;
            ViewBag.Percentage = total == 0 ? 0 : Math.Round((double)score / total * 100, 2);

            return View("ExamResult");
        }
        [HttpGet]
        public async Task<IActionResult> ExportExamTxt(int id)
        {
            var examRequest = await _examRequestService.GetByIdAsync(id);
            if (examRequest == null)
                return NotFound();

            var sb = new StringBuilder();
            sb.AppendLine($"--- امتحان منصة Throb Academy - المعرف: {id} ---");
            sb.AppendLine($"عدد الأسئلة: {examRequest.Questions?.Count ?? 0}");
            sb.AppendLine("=======================================\n");

            int index = 1;
            if (examRequest.Questions != null)
            {
                foreach (var question in examRequest.Questions)
                {
                    sb.AppendLine($"{index}. السؤال: {question.QuestionText}");
                    sb.AppendLine($"   (النوع: {question.QuestionType}, الصعوبة: {question.Difficulty})");

                    // إضافة الخيارات (لأسئلة الاختيار من متعدد)
                    if (question.Options != null && question.Options.Any())
                    {
                        sb.AppendLine("   الخيارات:");
                        foreach (var opt in question.Options.Cast<Throb.Data.Entities.QuestionOption>())
                        {
                            sb.AppendLine($"    - {opt.OptionText}");
                        }
                    }

                    // إضافة الإجابة الصحيحة
                    sb.AppendLine($"   الإجابة الصحيحة: {question.CorrectAnswer}");
                    sb.AppendLine("---------------------------------------");
                    index++;
                }
            }

            var contentBytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(contentBytes, "text/plain", $"Exam_{id}.txt");
        }
    }
}