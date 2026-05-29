using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using IT_Gied.Models;

namespace IT_Gied.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["AIProvider:ApiKey"] ?? "";
            _model = configuration["AIProvider:Model"] ?? "gemini-2.5-flash";
        }

        public async Task<AiScheduleResult> GenerateAiScheduleAsync(
            double gpa,
            int requestedCredits,
            List<AiCourseInput> availableCourses,
            List<AiCompletedCourseInput> completedCourses,
            string? additionalNotes = null)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new AiScheduleResult { Explanation = "لم يتم ضبط مفتاح Gemini API." };

            if (!availableCourses.Any())
                return new AiScheduleResult { Explanation = "لا توجد مواد متاحة للتسجيل حالياً." };

            var coursesBlock = new StringBuilder();
            foreach (var c in availableCourses)
                coursesBlock.AppendLine($"ID:{c.CourseId} | {c.Name} | {c.Code} | {c.CreditHours}ساعات | صعوبة:{c.AverageDifficulty:0.0}/10 | {c.Status} | {c.Description ?? ""}");

            var gradesSection = completedCourses.Any()
                ? string.Join("\n", completedCourses.Select(g => $"- {g.Name} ({g.Code}): {g.Grade}"))
                : "لا توجد مواد مكتملة بعد.";

            var notesLine = string.IsNullOrWhiteSpace(additionalNotes) ? "" : $"\nملاحظات الطالب: {additionalNotes}";

            var prompt = $@"أنت مستشار أكاديمي. اختر مواد للطالب للفصل القادم.

GPA: {gpa:0.00}
الساعات المطلوبة: {requestedCredits}{notesLine}

علامات الطالب السابقة:
{gradesSection}

المواد المتاحة:
{coursesBlock}

القواعد:
- اختر مواد مجموعها {requestedCredits} ساعة بالضبط.
- إذا كان من المستحيل اختيار مجموع الساعات المطلوب بدقة، اختر أقرب مجموعة بالحد الأدنى من الانحراف، ووضح السبب في EXPLANATION.
- اعتمد على الصعوبة (AverageDifficulty) كمعيار رئيسي.
- GPA منخفض = اختر مواد صعوبة أقل.
- لا تضع أكثر من مادتين صعوبة فوق 7.
- Retake لها أولوية.
- يجب اختيار مواد دائماً.

أرجع هذا النص بالضبط مع تعبئة الأرقام:
SELECTED_IDS: 1,2,3
TOTAL_CREDITS: 9
EXPLANATION: السبب هنا بالعربي";

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 512
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new AiScheduleResult { Explanation = $"خطأ من Gemini: {response.ReasonPhrase}" };

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                var rawText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text?.Trim() ?? "";

                return ParseTextResponse(rawText, availableCourses, requestedCredits);
            }
            catch (Exception ex)
            {
                return new AiScheduleResult { Explanation = $"خطأ: {ex.Message}" };
            }
        }

        private AiScheduleResult ParseTextResponse(string text, List<AiCourseInput> availableCourses, int requestedCredits)
        {
            var result = new AiScheduleResult();
            var availableIds = availableCourses.Select(c => c.CourseId).ToHashSet();

            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("SELECTED_IDS:"))
                {
                    var idsStr = trimmed.Replace("SELECTED_IDS:", "").Trim();
                    foreach (var part in idsStr.Split(','))
                    {
                        if (int.TryParse(part.Trim(), out int id) && availableIds.Contains(id))
                            result.SelectedCourseIds.Add(id);
                    }
                }
                else if (trimmed.StartsWith("TOTAL_CREDITS:"))
                {
                    if (int.TryParse(trimmed.Replace("TOTAL_CREDITS:", "").Trim(), out int credits))
                        result.TotalCredits = credits;
                }
                else if (trimmed.StartsWith("EXPLANATION:"))
                {
                    result.Explanation = trimmed.Replace("EXPLANATION:", "").Trim();
                }
            }

            if (!result.SelectedCourseIds.Any() || result.TotalCredits != requestedCredits)
            {
                var fallbackCourseIds = FindBestCreditMatch(availableCourses, requestedCredits);
                if (fallbackCourseIds.Any())
                {
                    result.SelectedCourseIds = fallbackCourseIds;
                    result.TotalCredits = availableCourses
                        .Where(c => fallbackCourseIds.Contains(c.CourseId))
                        .Sum(c => c.CreditHours);

                    if (result.TotalCredits == requestedCredits)
                    {
                        result.Explanation = string.IsNullOrWhiteSpace(result.Explanation)
                            ? "تم اختيار المواد تلقائياً لتطابق عدد الساعات المطلوب بدقة."
                            : result.Explanation + " تم تعديل الاختيار ليطابق عدد الساعات المطلوب بدقة.";
                    }
                    else
                    {
                        result.Explanation = string.IsNullOrWhiteSpace(result.Explanation)
                            ? $"تم اختيار المواد تلقائياً لتقريب عدد الساعات إلى {result.TotalCredits} بدل {requestedCredits}."
                            : result.Explanation + $" تم تعديل الاختيار ليقرب عدد الساعات إلى {result.TotalCredits} بدل {requestedCredits}.";
                    }
                }
                else if (!result.SelectedCourseIds.Any())
                {
                    var fallback = availableCourses
                        .OrderBy(c => c.AverageDifficulty)
                        .Take(4)
                        .ToList();

                    result.SelectedCourseIds = fallback.Select(c => c.CourseId).ToList();
                    result.TotalCredits = fallback.Sum(c => c.CreditHours);
                    result.Explanation = "تم اختيار المواد تلقائياً بناءً على الصعوبة الأقل.";
                }
            }

            return result;
        }

        private List<int> FindBestCreditMatch(List<AiCourseInput> availableCourses, int requestedCredits)
        {
            var dp = new Dictionary<int, List<int>>
            {
                [0] = new List<int>()
            };

            foreach (var course in availableCourses)
            {
                var sums = dp.Keys.OrderByDescending(k => k).ToList();
                foreach (var sum in sums)
                {
                    var newSum = sum + course.CreditHours;
                    if (newSum > requestedCredits + 3)
                        continue;

                    if (!dp.ContainsKey(newSum))
                    {
                        dp[newSum] = new List<int>(dp[sum]) { course.CourseId };
                    }
                }
            }

            if (dp.TryGetValue(requestedCredits, out var exactMatch))
                return exactMatch;

            var closest = dp.Keys
                .OrderBy(k => Math.Abs(k - requestedCredits))
                .ThenBy(k => k < requestedCredits ? 0 : 1)
                .FirstOrDefault();

            return closest > 0 ? dp[closest] : new List<int>();
        }

        public async Task<string> GetPersonalizedAdviceAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return "لم يتم ضبط مفتاح Gemini API.";

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new {
                            parts = new[] {
                                new { text = "أنت مرشد أكاديمي خبير. اجعل ردك بالعربي دافئاً ومحفزاً." },
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return $"خطأ: {response.ReasonPhrase}";

                var jsonResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                return jsonResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "تعذر الحصول على رد.";
            }
            catch (Exception ex)
            {
                return $"خطأ تقني: {ex.Message}";
            }
        }
    }

    public class AiCourseInput
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public int CreditHours { get; set; }
        public string? Description { get; set; }
        public double AverageDifficulty { get; set; }
        public string Status { get; set; } = "New";
    }

    public class AiCompletedCourseInput
    {
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string? Grade { get; set; }
    }

    public class AiScheduleResult
    {
        [JsonPropertyName("selectedCourseIds")]
        public List<int> SelectedCourseIds { get; set; } = new();

        [JsonPropertyName("totalCredits")]
        public int TotalCredits { get; set; }

        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = "";
    }

    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
