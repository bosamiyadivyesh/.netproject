// Premium Admin UI enhancements — presentation only
(function () {
    // 1) Highlight active sidebar link
    var path = location.pathname.toLowerCase();
    document.querySelectorAll(".admin-sidebar nav a").forEach(function (a) {
        var href = (a.getAttribute("href") || "").toLowerCase();
        if (href && path.indexOf(href) !== -1) a.classList.add("active");
    });

    // 2) Auto-badge status / priority / role cells
    var map = {
        pending: "warn", in_progress: "info", completed: "success",
        cancelled: "muted", low: "muted", medium: "info",
        high: "warn", urgent: "danger",
        read: "muted", unread: "info",
        admin: "danger", user: "info"
    };
    document.querySelectorAll(".admin-content td").forEach(function (td) {
        if (td.children.length > 0) return;
        var text = td.textContent.trim().toLowerCase();
        if (map[text]) {
            td.innerHTML = '<span class="abadge abadge-' + map[text] + '">' +
                text.replace("_", " ") + "</span>";
        }
    });
})();