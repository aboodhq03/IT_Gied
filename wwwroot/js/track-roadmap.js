(function () {
    const nodesWrap = document.getElementById("nodes");
    const edgesSvg = document.getElementById("edges");

    const wrap = document.querySelector(".roadmap-wrap");
    const trackSlug = wrap ? (wrap.getAttribute("data-track-slug") || "webdev") : "webdev";

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const antiForgeryToken = tokenInput ? tokenInput.value : "";

    // ===== Links from DB =====
    let linksCache = null;
    async function fetchLinks() {
        if (linksCache) return linksCache;
        const res = await fetch(`/Tracks/Links?slug=${encodeURIComponent(trackSlug)}`);
        if (!res.ok) throw new Error("Failed to load links");
        linksCache = await res.json();
        return linksCache;
    }

    function getNodeCenter(nodeEl) {
        return {
            x: nodeEl.offsetLeft + nodeEl.offsetWidth / 2,
            y: nodeEl.offsetTop + nodeEl.offsetHeight / 2
        };
    }

    function clearEdges() {
        edgesSvg.innerHTML = "";
    }

    function makeLine(x1, y1, x2, y2, cssClass = "") {
        const line = document.createElementNS("http://www.w3.org/2000/svg", "line");
        line.setAttribute("x1", x1);
        line.setAttribute("y1", y1);
        line.setAttribute("x2", x2);
        line.setAttribute("y2", y2);

        line.classList.add("edge");
        if (cssClass && typeof cssClass === "string") {
            const tokens = cssClass.trim().split(/\s+/).filter(Boolean);
            for (const t of tokens) {
                if (t !== "edge") line.classList.add(t);
            }
        }
        return line;
    }

    function getCompletedSet() {
        return new Set(
            Array.from(document.querySelectorAll(".node.completed"))
                .map(n => parseInt(n.getAttribute("data-id"), 10))
                .filter(Boolean)
        );
    }

    async function refreshStates() {
        const links = await fetchLinks();

        // prereq: to -> [from]
        const prereqMap = new Map();
        for (const l of links) {
            if (l.type !== "Prerequisite") continue;
            if (!prereqMap.has(l.to)) prereqMap.set(l.to, []);
            prereqMap.get(l.to).push(l.from);
        }

        const completedSet = getCompletedSet();

        const allNodes = Array.from(document.querySelectorAll(".node"));
        for (const node of allNodes) {
            const id = parseInt(node.getAttribute("data-id"), 10);
            if (!id) continue;

            const btn = node.querySelector(".btn-done");

            if (node.classList.contains("completed")) {
                if (btn) { btn.disabled = false; btn.textContent = "إلغاء"; }
                continue;
            }

            const prereqs = prereqMap.get(id) || [];
            const isUnlocked = prereqs.length === 0 || prereqs.every(p => completedSet.has(p));

            node.classList.remove("locked", "unlocked");
            node.classList.add(isUnlocked ? "unlocked" : "locked");

            if (btn) {
                btn.disabled = !isUnlocked;
                btn.textContent = "إنجاز";
            }
        }
    }

    async function drawEdges() {
        const links = await fetchLinks();

        const w = nodesWrap.scrollWidth;
        const h = nodesWrap.scrollHeight;

        edgesSvg.setAttribute("width", w);
        edgesSvg.setAttribute("height", h);
        edgesSvg.setAttribute("viewBox", `0 0 ${w} ${h}`);

        clearEdges();

        const completedSet = getCompletedSet();

        for (const l of links) {
            const fromEl = document.querySelector(`.node[data-id="${l.from}"]`);
            const toEl = document.querySelector(`.node[data-id="${l.to}"]`);
            if (!fromEl || !toEl) continue;

            const a = getNodeCenter(fromEl);
            const b = getNodeCenter(toEl);

            const typeClass = (l.type === "Concurrent") ? "edge-concurrent" : "edge-prereq";
            const isActive = completedSet.has(l.from) || completedSet.has(l.to);
            const cssClass = isActive ? `${typeClass} active` : typeClass;

            edgesSvg.appendChild(makeLine(a.x, a.y, b.x, b.y, cssClass));
        }
    }

    async function toggleCompleteAjax(unitId) {
        const formData = new FormData();
        formData.append("unitId", unitId);
        formData.append("__RequestVerificationToken", antiForgeryToken);

        const res = await fetch("/Tracks/ToggleCompleteAjax", {
            method: "POST",
            body: formData
        });

        if (!res.ok) throw new Error("Toggle request failed");
        const data = await res.json();
        if (!data.ok) throw new Error(data.message || "Toggle failed");
        return data;
    }

    nodesWrap.addEventListener("click", async (e) => {
        const btn = e.target.closest(".btn-done");
        if (!btn) return;

        e.preventDefault();

        const nodeEl = btn.closest(".node");
        const unitId = nodeEl ? parseInt(nodeEl.getAttribute("data-id"), 10) : 0;
        if (!unitId) return;

        try {
            btn.disabled = true;
            const data = await toggleCompleteAjax(unitId);

            if (data.isCompleted) {
                nodeEl.classList.add("completed");
                nodeEl.classList.remove("locked", "unlocked");
                btn.textContent = "إلغاء";
            } else {
                nodeEl.classList.remove("completed");
                btn.textContent = "إنجاز";
            }

            await refreshStates();
            await drawEdges();
        } catch (err) {
            alert(err.message || "Error");
        } finally {
            const isLocked = nodeEl.classList.contains("locked");
            btn.disabled = isLocked;
        }
    });

    window.addEventListener("resize", () => drawEdges());

    (async function init() {
        await refreshStates();
        await drawEdges();
    })();
})();
