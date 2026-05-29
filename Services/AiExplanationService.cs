using IT_Gied.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IT_Gied.Services
{
    public class AiExplanationService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AiExplanationService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<string> GenerateExplanation(
            RecommendationResult result,
            double? gpa,
            int allowedCreditHours,
            string? careerGoal)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("أنت مرشد أكاديمي للطلاب الجامعيين. يرجى تقديم شرح ونصيحة أكاديمية باللغة العربية بناءً على بيانات الطالب ومعدله التراكمي والمقررات الموصى بها.");
            
            if (!string.IsNullOrWhiteSpace(careerGoal))
            {
                promptBuilder.AppendLine($"هدف الطالب المهني هو: {careerGoal}. يرجى ربط النصيحة بهذا الهدف وتوضيح كيف تساعد هذه المقررات في تحقيقه.");
            }

            promptBuilder.AppendLine("ملاحظة هامة جداً: لا تقم بتغيير أو اقتراح أي مقررات أخرى، فقط اشرح القائمة المقدمة بناءً على حالة الطالب.");
            promptBuilder.AppendLine($"المعدل التراكمي للطالب (GPA): {gpa}");
            promptBuilder.AppendLine($"الساعات المسموح بها لهذا الفصل: {allowedCreditHours}");
            promptBuilder.AppendLine("المقررات الموصى بها:");
            
            bool hasFailedCourses = false;
            foreach (var course in result.Courses)
            {
                promptBuilder.AppendLine($"- {course.Code}: {course.Name} ({course.CreditHours} ساعات) [درجة الصعوبة: {course.AverageDifficulty:0.0}/10] - الحالة: {course.Status}");
                if (course.Status == "Retake")
                    hasFailedCourses = true;
            }

            if (hasFailedCourses)
            {
                promptBuilder.AppendLine("\nملاحظة: لدى الطالب مقررات معادة (Retake). يرجى تقديم نصيحة حول كيفية تحسين مستواه الأكاديمي والتعامل مع هذه المقررات.");
            }

            return await CallGeminiAsync(promptBuilder.ToString());
        }

        public async Task<string> AnswerStudentQuestion(
            string question,
            double gpa,
            List<Course> completedCourses,
            List<Course> failedCourses,
            List<RecommendedCourseDto> recommendedCourses,
            string? careerGoal)
        {
            var preferences = ParseStudentQuestion(question);
            var filteredRecommendedCourses = FilterRecommendedCourses(recommendedCourses, preferences);
            var excludedCourseNames = recommendedCourses
                .Where(rc => !filteredRecommendedCourses.Select(c => c.CourseId).Contains(rc.CourseId))
                .Select(rc => rc.Name)
                .ToList();

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("أنت مرشد أكاديمي متعاون للطلاب الجامعيين. يجب أن تكون إجاباتك باللغة العربية فقط.");
            promptBuilder.AppendLine("لديك سياق عن الطالب والمقررات الموصى بها فقط. لا تذكر أو تقترح أي مادة غير موجودة في قائمة المقررات الموصى بها.");
            promptBuilder.AppendLine("إذا كان الطالب استبعد موضوعاً مثل رياضيات أو شبكات، لا تستخدم أي مادة تحمل هذا الموضوع في الاسم أو الوصف.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("⭐ المبدأ الأساسي: درجة الصعوبة هي العامل الأساسي في تقييمك للمواد والنصائح المقدمة.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("قواعد تصنيف صعوبة المواد التي يجب الالتزام بها في ردودك:");
            promptBuilder.AppendLine("- درجة صعوبة من 1.0 إلى 4.9 من 10 => تصنيف: 🟢 سهل");
            promptBuilder.AppendLine("- درجة صعوبة من 5.0 إلى 6.9 من 10 => تصنيف: 🟡 متوسط");
            promptBuilder.AppendLine("- درجة صعوبة من 7.0 إلى 10.0 من 10 => تصنيف: 🔴 صعب");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("📋 تحليل طلبات الطالب من السؤال:");
            promptBuilder.AppendLine("1. المواد المرغوبة: مثل 'بدي برمجة' أو 'أريد برمجة'.");
            promptBuilder.AppendLine("2. المواد المستبعدة: مثل 'ما بدي رياضيات' أو 'بدون شبكات'.");
            promptBuilder.AppendLine("3. درجات الصعوبة المطلوبة: مثل 'صعب'، 'سهل'، 'متوسط'.");
            promptBuilder.AppendLine("4. عدد المواد المطلوب: مثل 'مادتين' أو 'ثلاث مواد'.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("متطلبات الإجابة:");
            promptBuilder.AppendLine("1. عند الحديث عن أي مادة، اذكر دائماً تصنيفها (سهل/متوسط/صعب) مع درجة الصعوبة الرقمية.");
            promptBuilder.AppendLine("2. احترم طلبات الطالب بشكل حرفي:");
            promptBuilder.AppendLine("   - إذا قال 'بدي برمجة'، ركز على مواد برمجية.");
            promptBuilder.AppendLine("   - إذا قال 'ما بدي رياضيات'، استبعد مواد الرياضيات تماماً.");
            promptBuilder.AppendLine("   - إذا قال 'ما بدي شبكات'، استبعد مواد الشبكات تماماً.");
            promptBuilder.AppendLine("   - إذا قال 'بدي مادتين صعبات' أو 'بدي مادة صعبة'، قدم مواد صعبة مناسبة.");
            promptBuilder.AppendLine("3. لا تذكر أو تقترح أي مادة خارج القائمة الموصى بها.");
            promptBuilder.AppendLine("4. لا تذكر أي مادة تم استبعادها وفقاً للوصف أو الموضوع المحدد.");
            promptBuilder.AppendLine("5. قيّم توصياتك بناءً على الصعوبة أولاً: هل توازن المواد حسب مستوى الصعوبة مناسب لـ GPA الطالب؟");
            promptBuilder.AppendLine("6. إذا كان الفصل يحتوي على أكثر من مادتين تصنيفهما صعب (7.0+)، نبّه الطالب بوضوح أن العبء ثقيل جداً.");
            promptBuilder.AppendLine("8. إذا كان GPA الطالب أقل من 2.5، أعطِ أولوية للمواد سهلة/متوسطة وتجنب المواد الصعبة قدر الإمكان.");
            promptBuilder.AppendLine("9. إذا كان GPA الطالب بين 2.5 و3.5، اجمع بين مواد صعبة وسهلة لتحقيق توازن مناسب.");
            promptBuilder.AppendLine("10. إذا كان GPA الطالب فوق 3.5، يمكن اقتراح مواد أكثر صعوبة مع توضيح أن الطالب قادر على التعامل معها.");
            promptBuilder.AppendLine("11. استخدم وصف المادة (Description) لتحديد محتواها وتصنيفها بدقة.");
            promptBuilder.AppendLine("12. اربط كل نصيحة بدرجة الصعوبة المقابلة للمادة المذكورة.");
            promptBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(careerGoal))
            {
                promptBuilder.AppendLine($"هدف الطالب المهني هو: {careerGoal}.");
            }

            promptBuilder.AppendLine($"المعدل التراكمي للطالب (GPA): {gpa}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"المواد المطلوبة من الطالب: {(preferences.RequiredTopics.Any() ? string.Join(", ", preferences.RequiredTopics) : "لا توجد مواد محددة")} ");
            promptBuilder.AppendLine($"المواد المستبعدة من الطالب: {(preferences.ExcludedTopics.Any() ? string.Join(", ", preferences.ExcludedTopics) : "لا توجد مواد مستبعدة")}");
            promptBuilder.AppendLine($"الطلب الخاص بالصعوبة: {(string.IsNullOrEmpty(preferences.DifficultyRequest) ? "غير محدد" : preferences.DifficultyRequest)}");
            if (preferences.CountRequest.HasValue)
                promptBuilder.AppendLine($"عدد المواد المطلوب: {preferences.CountRequest.Value}");
            promptBuilder.AppendLine($"سؤال الملائمة: {(preferences.AsksIfSuitable ? "نعم" : "لا")} ");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"المواد التي تم استبعادها بناءً على طلب الطالب: {(excludedCourseNames.Any() ? string.Join(", ", excludedCourseNames) : "لا توجد مواد مستبعدة")}.");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("المقررات التي اجتازها الطالب:");
            foreach (var c in completedCourses) promptBuilder.AppendLine($"- {c.Code}: {c.Name}");
            if (!completedCourses.Any()) promptBuilder.AppendLine("- لا يوجد");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("المقررات التي رسب فيها أو يحتاج إعادتها:");
            foreach (var c in failedCourses) promptBuilder.AppendLine($"- {c.Code}: {c.Name}");
            if (!failedCourses.Any()) promptBuilder.AppendLine("- لا يوجد");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("المقررات الموصى بها للفصل القادم مع درجة صعوبتها وتصنيفها:");
            var difficultCourses = filteredRecommendedCourses.Count(rc => rc.AverageDifficulty >= 7.0);
            var easyCourses = filteredRecommendedCourses.Count(rc => rc.AverageDifficulty < 5.0);
            var moderateCourses = filteredRecommendedCourses.Count(rc => rc.AverageDifficulty >= 5.0 && rc.AverageDifficulty < 7.0);

            foreach (var rc in filteredRecommendedCourses)
            {
                string classification = rc.AverageDifficulty >= 7.0 ? "🔴 صعب"
                                       : rc.AverageDifficulty >= 5.0 ? "🟡 متوسط"
                                       : rc.AverageDifficulty >  0   ? "🟢 سهل"
                                       : "غير مقيّم";
                string descriptionPart = string.IsNullOrWhiteSpace(rc.Description) ? "" : $" | الوصف: {rc.Description}";
                promptBuilder.AppendLine($"- {rc.Code}: {rc.Name} [درجة الصعوبة: {rc.AverageDifficulty:0.0}/10 - {classification}]{descriptionPart}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"📊 ملخص توزيع المواد حسب الصعوبة:");
            promptBuilder.AppendLine($"   • 🟢 سهل: {easyCourses} مواد");
            promptBuilder.AppendLine($"   • 🟡 متوسط: {moderateCourses} مواد");
            promptBuilder.AppendLine($"   • 🔴 صعب: {difficultCourses} مواد");

            if (difficultCourses > 2)
            {
                promptBuilder.AppendLine($"\n⚠️ تنبيه مهم: هناك {difficultCourses} مواد صعبة (7.0+) في الخطة الموصى بها. هذا عبء ثقيل جداً!");
                promptBuilder.AppendLine("   يُنصح بإعادة النظر في اختيار المواد لتحقيق توازن أفضل.");
            }

            if (easyCourses == 0 && gpa < 2.5)
            {
                promptBuilder.AppendLine($"\n⚠️ تنبيه: المعدل الحالي ({gpa}) منخفض وليس هناك مواد سهلة في الخطة. يُنصح بإضافة مواد سهلة لبناء المعدل.");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"🎯 سؤال الطالب هو: {question}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("✅ المطلوب من الرد:");
            promptBuilder.AppendLine("1. أجب بشكل مباشر عن سؤال الطالب بناءً على طلبه.");
            promptBuilder.AppendLine("2. استخدم فقط المواد أعلاه في الإجابة ولا تقترح مواد جديدة.");
            promptBuilder.AppendLine("3. إذا قال 'ما بدي رياضيات'، لا تذكر أي مادة رياضيات.");
            promptBuilder.AppendLine("4. إذا قال 'بدي برمجة'، ركز على مواد البرمجة المتاحة.");
            promptBuilder.AppendLine("5. إذا طلب مواد صعبة وسهلة، قارن بين عدد المواد الصعبة والسهلة الموجودة.");
            promptBuilder.AppendLine("6. قدم شرحاً موجزاً عن مدى توازن هذه الخطة مع GPA الطالب.");
            promptBuilder.AppendLine("7. لا تخلط بين شروط الطالب: أولاً انتبه للمواد المطلوبة، ثم للمواد المستبعدة، ثم للصعوبة.");
            promptBuilder.AppendLine("8. رتب الإجابة في أقسام واضحة: (أ) ما طلبه الطالب، (ب) المواد المناسبة، (ج) سبب الاختيار، (د) ملاحظة عن التوازن والصعوبة.");
            promptBuilder.AppendLine("9. إذا كانت أي طلبات متضاربة أو غير ممكنة، اذكر ذلك بوضوح بدلاً من خلط الإجابة.");

            return await CallGeminiAsync(promptBuilder.ToString());
        }

        private (List<string> RequiredTopics, List<string> ExcludedTopics, string DifficultyRequest, int? CountRequest, bool AsksIfSuitable) ParseStudentQuestion(string question)
        {
            var required = new List<string>();
            var excluded = new List<string>();
            string difficulty = string.Empty;
            int? count = null;
            bool asksSuitable = false;

            if (string.IsNullOrWhiteSpace(question))
                return (required, excluded, difficulty, count, asksSuitable);

            var lower = question.ToLowerInvariant();

            if (lower.Contains("برمجة") || lower.Contains("برمج"))
                required.Add("برمجة");
            if (lower.Contains("رياضيات") || lower.Contains("math") || lower.Contains("حساب"))
                excluded.Add("رياضيات");
            if (lower.Contains("شبكات") || lower.Contains("network"))
                excluded.Add("شبكات");
            if (lower.Contains("قواعد") || lower.Contains("db"))
                excluded.Add("قواعد البيانات");

            if (lower.Contains("صعب"))
                difficulty = "صعب";
            else if (lower.Contains("سهل"))
                difficulty = "سهل";
            else if (lower.Contains("متوسط"))
                difficulty = "متوسط";

            if (lower.Contains("مادتين") || lower.Contains("٢") || lower.Contains("2") || lower.Contains("اثنين"))
                count = 2;
            else if (lower.Contains("ثلاث مواد") || lower.Contains("٣") || lower.Contains("3") || lower.Contains("ثلاثة"))
                count = 3;
            else if (lower.Contains("مادة"))
                count ??= 1;

            if (lower.Contains("هل هذه الخطة") || lower.Contains("هل الخطة") || lower.Contains("مناسبة لي") || lower.Contains("مناسبة"))
                asksSuitable = true;

            return (required, excluded, difficulty, count, asksSuitable);
        }

        private List<RecommendedCourseDto> FilterRecommendedCourses(
            List<RecommendedCourseDto> recommendedCourses,
            (List<string> RequiredTopics, List<string> ExcludedTopics, string DifficultyRequest, int? CountRequest, bool AsksIfSuitable) preferences)
        {
            var filtered = new List<RecommendedCourseDto>();

            foreach (var course in recommendedCourses)
            {
                var tags = GetCourseTags(course);

                if (preferences.ExcludedTopics.Any(ex => tags.Contains(ex)))
                    continue;

                if (preferences.RequiredTopics.Any() && !preferences.RequiredTopics.Any(req => tags.Contains(req)))
                    continue;

                filtered.Add(course);
            }

            return filtered;
        }

        private HashSet<string> GetCourseTags(RecommendedCourseDto course)
        {
            var text = $"{course.Name} {course.Code} {course.Description}".ToLowerInvariant();
            var tags = new HashSet<string>();

            if (text.Contains("برمجة") || text.Contains("برمج") || text.Contains("coding") || text.Contains("software") || text.Contains("development"))
                tags.Add("برمجة");
            if (text.Contains("رياضيات") || text.Contains("math") || text.Contains("حساب") || text.Contains("تفاضل") || text.Contains("تكامل") || text.Contains("إحصاء"))
                tags.Add("رياضيات");
            if (text.Contains("شبكات") || text.Contains("network") || text.Contains("نتورك") || text.Contains("routing") || text.Contains("switch") || text.Contains("packet"))
                tags.Add("شبكات");
            if (text.Contains("قواعد البيانات") || text.Contains("database") || text.Contains("db") || text.Contains("sql") || text.Contains("mysql") || text.Contains("oracle"))
                tags.Add("قواعد البيانات");

            return tags;
        }

        public async Task<string> GenerateGraduationPlan(
            double gpa,
            int targetSemesters,
            List<Course> completedCourses,
            List<Course> failedCourses,
            List<Course> allCourses,
            List<CoursePrerequisite> prerequisites)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("أنت مرشد أكاديمي خبير. مهمتك هي إنشاء خطة تخرج فصلية للطالب باللغة العربية بناءً على المعطيات التالية.");
            promptBuilder.AppendLine("يجب أن تكون الخطة موزعة على فصول دراسية، مع مراعاة المقررات التي اجتازها الطالب، والمقررات التي رسب فيها، والمتطلبات السابقة للمقررات (Prerequisites).");
            
            promptBuilder.AppendLine($"\nبيانات الطالب:");
            promptBuilder.AppendLine($"- المعدل التراكمي (GPA): {gpa}");
            promptBuilder.AppendLine($"- الفصول الدراسية المستهدفة للخطة: {targetSemesters} فصول (قم بتوزيع المقررات المتبقية على هذا العدد من الفصول بشكل منطقي قدر الإمكان).");

            promptBuilder.AppendLine("\nالمقررات التي اجتازها الطالب:");
            foreach (var c in completedCourses) promptBuilder.AppendLine($"- {c.Code}: {c.Name}");
            if (!completedCourses.Any()) promptBuilder.AppendLine("- لا يوجد");

            promptBuilder.AppendLine("\nالمقررات التي رسب فيها ويجب إعادتها:");
            foreach (var c in failedCourses) promptBuilder.AppendLine($"- {c.Code}: {c.Name}");
            if (!failedCourses.Any()) promptBuilder.AppendLine("- لا يوجد");

            promptBuilder.AppendLine("\nقائمة جميع المقررات المتاحة في الخطة الدراسية:");
            foreach (var c in allCourses) promptBuilder.AppendLine($"- {c.Code}: {c.Name} ({c.CreditHours} ساعات)");

            promptBuilder.AppendLine("\nالمتطلبات السابقة للمقررات:");
            var prereqLookup = prerequisites.GroupBy(p => p.CourseId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var courseId in prereqLookup.Keys)
            {
                var course = allCourses.FirstOrDefault(c => c.Id == courseId);
                var prereqCodes = prereqLookup[courseId]
                    .Select(p => allCourses.FirstOrDefault(c => c.Id == p.PrerequisiteCourseId)?.Code)
                    .Where(c => c != null)
                    .ToList();
                
                if (course != null && prereqCodes.Any())
                {
                    promptBuilder.AppendLine($"- المقرر {course.Code} يتطلب اجتياز: {string.Join(", ", prereqCodes)}");
                }
            }

            promptBuilder.AppendLine("\nالتعليمات:");
            promptBuilder.AppendLine("1. قم بإنشاء خطة تخرج واضحة ومنسقة مقسمة إلى فصول دراسية (الفصل الأول، الفصل الثاني، وهكذا).");
            promptBuilder.AppendLine("2. تأكد من عدم تخطي المتطلبات السابقة (Prerequisites).");
            promptBuilder.AppendLine("3. اعتنِ بتوزيع العبء الدراسي بحيث لا يتجاوز 18 ساعة في الفصل الواحد، وراعِ المعدل التراكمي للطالب (إذا كان المعدل أقل من 2.0 تجنب زيادة العبء عن 12 ساعة).");
            promptBuilder.AppendLine("4. اشرح سبب اختيارك لترتيب المقررات باختصار بعد كل فصل.");
            promptBuilder.AppendLine("5. اجعل الإجابة مرتبة، ومحفزة للطالب.");

            return await CallGeminiAsync(promptBuilder.ToString());
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var apiKey = _config["AIProvider:ApiKey"];
            var model = _config["AIProvider:Model"] ?? "gemini-1.5-flash";

            if (string.IsNullOrEmpty(apiKey))
            {
                return "لم يتم تكوين مفتاح API للذكاء الاصطناعي.";
            }

            var url = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = "أنت مرشد أكاديمي متعاون. يجب أن تكون إجاباتك باللغة العربية فقط." },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new {
                    temperature = 0.7
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(url, requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var candidates = jsonDoc.RootElement.GetProperty("candidates");
                    if (candidates.GetArrayLength() > 0)
                    {
                        var content = candidates[0].GetProperty("content");
                        var parts = content.GetProperty("parts");
                        if (parts.GetArrayLength() > 0)
                        {
                            return parts[0].GetProperty("text").GetString() ?? "تعذر الحصول على إجابة.";
                        }
                    }
                    return "تعذر الحصول على إجابة.";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {response.StatusCode} - {errorResponse}");
                    return "عذراً، لم نتمكن من جلب إجابة من الذكاء الاصطناعي في الوقت الحالي بسبب مشكلة في الخادم.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini API Exception: {ex.Message}");
                return "عذراً، حدث خطأ أثناء الاتصال بخدمة الذكاء الاصطناعي.";
            }
        }
    }
}