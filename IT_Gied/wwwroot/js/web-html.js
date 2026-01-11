const QUIZ_SIZE = 10;
let currentQuiz = [];

function pickRandom(arr, count) {
    const copy = [...arr];
    for (let i = copy.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [copy[i], copy[j]] = [copy[j], copy[i]];
    }
    return copy.slice(0, Math.min(count, copy.length));
}

function renderQuiz() {
    const area = document.getElementById("quizArea");
    const all = document.getElementById("r-all");

    if (!area) return;

    // إذا بنك الأسئلة مش موجود اعرض رسالة بدل الفراغ
    if (!window.questionBank || !Array.isArray(window.questionBank) || window.questionBank.length === 0) {
        area.innerHTML = `
      <div class="result bad">
        <strong>الأسئلة غير محمّلة.</strong>
        <div class="muted" style="margin-top:6px;">
          تأكد أن ملف <code class="inline">questions.js</code> موجود بنفس مجلد الصفحة،
          وأنه مرتبط قبل <code class="inline">app.js</code>.
        </div>
      </div>
    `;
        if (all) all.hidden = true;
        return;
    }

    if (all) all.hidden = true;

    currentQuiz = pickRandom(window.questionBank, QUIZ_SIZE);

    area.innerHTML = currentQuiz.map((q, idx) => {
        const name = `q_${q.id}`;
        return `
      <div class="q" data-id="${q.id}">
        <p class="q-title">${idx + 1}) ${q.q} <span class="muted">(${q.topic || "HTML"})</span></p>
        <div class="opts">
          ${q.o.map((opt, i) => `
            <label class="opt">
              <input type="radio" name="${name}" value="${i}">
              <span>${opt}</span>
            </label>
          `).join("")}
        </div>
        <div class="result" id="r_${q.id}" hidden></div>
      </div>
    `;
    }).join("");
}

function gradeAll() {
    if (!currentQuiz.length) return;

    let score = 0;

    currentQuiz.forEach(q => {
        const picked = document.querySelector(`input[name="q_${q.id}"]:checked`);
        const box = document.getElementById(`r_${q.id}`);
        if (!box) return;

        box.hidden = false;

        if (!picked) {
            box.className = "result bad";
            box.innerHTML = "<strong>لم تختر إجابة.</strong> اختر خيارًا ثم أعد التصحيح.";
            return;
        }

        const ok = Number(picked.value) === q.a;
        if (ok) score++;

        box.className = "result " + (ok ? "ok" : "bad");
        box.innerHTML = ok
            ? "<strong>صحيح.</strong>"
            : `<strong>خطأ.</strong> الإجابة الصحيحة: <span class="muted">${q.o[q.a]}</span>`;
    });

    const all = document.getElementById("r-all");
    if (all) {
        all.hidden = false;
        all.className = "result";
        all.innerHTML = `<strong>نتيجتك:</strong> ${score} / ${currentQuiz.length}`;
    }
}

function resetAll() {
    const area = document.getElementById("quizArea");
    if (!area) return;

    area.querySelectorAll('input[type="radio"]').forEach(r => r.checked = false);
    area.querySelectorAll('.result').forEach(el => el.hidden = true);

    const all = document.getElementById("r-all");
    if (all) all.hidden = true;
}

document.addEventListener("DOMContentLoaded", () => {
    const newQuizBtn = document.getElementById("newQuizBtn");
    const gradeBtn = document.getElementById("gradeBtn");
    const resetBtn = document.getElementById("resetBtn");

    if (newQuizBtn) newQuizBtn.addEventListener("click", renderQuiz);
    if (gradeBtn) gradeBtn.addEventListener("click", gradeAll);
    if (resetBtn) resetBtn.addEventListener("click", resetAll);

    renderQuiz();
});
