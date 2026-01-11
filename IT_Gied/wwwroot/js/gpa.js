(function () {

    // Normalize grade: trim, uppercase, remove spaces
    function normalizeGrade(s) {
        return (s || "")
            .toString()
            .trim()
            .toUpperCase()
            .replace(/\s+/g, "");
    }

    // Grade letters -> points (based on your university scale image)
    // A+ 4.2, A 4.0, A- 3.75, B+ 3.5, B 3.25, B- 3.0,
    // C+ 2.75, C 2.5, C- 2.25, D+ 2.0, D 1.75, D- 1.5, F 0.5
    function gradeToPoints(gradeRaw) {
        const g = normalizeGrade(gradeRaw);

        const map = {
            "A+": 4.2,
            "A": 4.0,
            "A-": 3.75,
            "B+": 3.5,
            "B": 3.25,
            "B-": 3.0,
            "C+": 2.75,
            "C": 2.5,
            "C-": 2.25,
            "D+": 2.0,
            "D": 1.75,
            "D-": 1.5,
            "F": 0.5
        };

        return map[g] ?? null; // null => invalid grade
    }

    const rows = document.getElementById("rows");
    const addRowBtn = document.getElementById("addRow");

    const oldGpaEl = document.getElementById("oldGpa");
    const oldCreditsEl = document.getElementById("oldCredits");

    const termCreditsEl = document.getElementById("termCredits");
    const termGpaEl = document.getElementById("termGpa");
    const newCumGpaEl = document.getElementById("newCumGpa");
    const hiddenCum = document.getElementById("cumulativeGpa");

    if (!rows || !addRowBtn || !oldGpaEl || !oldCreditsEl) return;

    function bindRow(tr) {
        const del = tr.querySelector(".del");
        if (del) {
            del.addEventListener("click", () => {
                if (rows.querySelectorAll("tr").length > 1) tr.remove();
                else {
                    tr.querySelector(".cname").value = "";
                    tr.querySelector(".credits").value = "";
                    tr.querySelector(".grade").value = "";
                }
                calculate();
            });
        }

        tr.querySelectorAll("input").forEach(inp => {
            inp.addEventListener("input", calculate);
        });
    }

    function calculate() {
        let termCredits = 0;
        let termPoints = 0;

        // remove previous invalid styles
        rows.querySelectorAll(".grade").forEach(i => i.classList.remove("invalid"));

        rows.querySelectorAll("tr").forEach(tr => {
            const cr = parseInt(tr.querySelector(".credits")?.value || "0", 10);
            const grade = tr.querySelector(".grade")?.value || "";

            if (!Number.isFinite(cr) || cr <= 0) return;

            const gp = gradeToPoints(grade);
            if (gp === null) {
                // Mark invalid grade visually, but don't break calculation
                const gradeEl = tr.querySelector(".grade");
                if (gradeEl && normalizeGrade(grade) !== "") gradeEl.classList.add("invalid");
                return;
            }

            termCredits += cr;
            termPoints += cr * gp;
        });

        const termGpa = termCredits > 0 ? (termPoints / termCredits) : 0;

        const oldGpa = parseFloat(oldGpaEl.value || "0");
        const oldCredits = parseInt(oldCreditsEl.value || "0", 10);

        const newCumGpa =
            ((oldGpa * oldCredits) + termPoints) /
            ((oldCredits + termCredits) || 1);

        termCreditsEl.textContent = termCredits;
        termGpaEl.textContent = termGpa.toFixed(2);
        newCumGpaEl.textContent = newCumGpa.toFixed(2);
        hiddenCum.value = newCumGpa.toFixed(2);
    }

    addRowBtn.addEventListener("click", () => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><input class="cname" type="text" placeholder="Course name" /></td>
            <td><input class="credits" type="number" min="0" step="1" /></td>
            <td><input class="grade" type="text" placeholder="e.g. A+ / b / c-" /></td>
            <td><button type="button" class="del">X</button></td>
        `;
        rows.appendChild(tr);
        bindRow(tr);
        calculate();
    });

    // init
    bindRow(rows.querySelector("tr"));
    oldGpaEl.addEventListener("input", calculate);
    oldCreditsEl.addEventListener("input", calculate);
    calculate();

})();
