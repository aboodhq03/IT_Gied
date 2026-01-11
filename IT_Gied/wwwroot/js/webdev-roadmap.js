(function () {
    const nodesWrap = document.getElementById("nodes");
    const edgesSvg = document.getElementById("edges");

    function getToken() {
        // الأفضل تحديده من الفورم المخفي حتى لا يلقط توكن ثاني بالغلط
        const el = document.querySelector('#__af input[name="__RequestVerificationToken"]')
            || document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function getNodeCenter(nodeEl) {
        const r = nodeEl.getBoundingClientRect();
        const pr = nodesWrap.getBoundingClientRect();
        return {
            x: (r.left - pr.left) + (r.width / 2) + nodesWrap.scrollLeft,
            y: (r.top - pr.top) + (r.height / 2) + nodesWrap.scrollTop
        };
    }

    function clearEdges() { edgesSvg.innerHTML = ""; }

    function makeLine(x1, y1, x2, y2, cssClass) {
        const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
        line.setAttribute("x1", x1);
        line.setAttribute("y1", y1);
        line.setAttribute("x2", x2);
        line.setAttribute("y2", y2);
        line.setAttribute("class", cssClass || "edge");
        edgesSvg.appendChild(line);
    }

    function drawEdges() {
        if (!window.links) return;

        clearEdges();
        edgesSvg.setAttribute("width", nodesWrap.scrollWidth);
        edgesSvg.setAttribute("height", nodesWrap.scrollHeight);

        window.links.forEach(l => {
            const fromId = l.FromNodeId ?? l.fromNodeId;
            const toId = l.ToNodeId ?? l.toNodeId;

            const fromEl = nodesWrap.querySelector(`.node[data-id="${fromId}"]`);
            const toEl = nodesWrap.querySelector(`.node[data-id="${toId}"]`);
            if (!fromEl || !toEl) return;

            const a = getNodeCenter(fromEl);
            const b = getNodeCenter(toEl);

            const fromCompleted = fromEl.classList.contains("completed");
            const toLocked = toEl.classList.contains("locked");
            const cls = fromCompleted ? "edge done" : (toLocked ? "edge locked" : "edge");

            makeLine(a.x, a.y, b.x, b.y, cls);
        });
    }

    // ✅ زر تم: POST FormData + AntiForgery (بدون JSON)
    window.toggleComplete = async function (nodeId) {
        try {
            const token = getToken();
            if (!token) {
                console.error("AntiForgery token not found in DOM.");
                alert("فشل حفظ التقدم (AntiForgery Token مفقود)");
                return;
            }

            const fd = new FormData();
            fd.append("id", Number(nodeId)); // لازم اسمها id لتطابق ToggleComplete(int id)
            fd.append("__RequestVerificationToken", token);

            const res = await fetch('/Tracks/ToggleComplete', {
                method: 'POST',
                body: fd
            });

            if (!res.ok) {
                const txt = await res.text().catch(() => "");
                console.error("ToggleComplete failed:", res.status, txt);
                alert('فشل حفظ التقدم (تحقق من Console)');
                return;
            }

            location.reload();
        } catch (e) {
            console.error(e);
            alert('فشل حفظ التقدم (تحقق من Console)');
        }
    };

    window.addEventListener("resize", drawEdges);
    nodesWrap.addEventListener("scroll", drawEdges);
    setTimeout(drawEdges, 0);
})();
