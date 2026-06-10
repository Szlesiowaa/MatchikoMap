(function () {
    function ensureContainer() {
        let container = document.getElementById("toast-container");

        if (!container) {
            container = document.createElement("div");
            container.id = "toast-container";
            document.body.appendChild(container);
        }

        return container;
    }

    window.showToast = function (message, duration = 3500) {
        const container = ensureContainer();

        const toast = document.createElement("div");
        toast.className = "toast";

        toast.innerHTML = `
            <div class="toast-message"></div>
            <div class="progress"></div>
        `;

        toast.querySelector('.toast-message').textContent = message;

        container.appendChild(toast);

        setTimeout(() => toast.classList.add("show"), 10);

        const progress = toast.querySelector(".progress");
        progress.offsetWidth;
        progress.style.transition = `transform ${duration}ms linear`;
        progress.style.transform = "scaleX(0)";

        const timeout = setTimeout(closeToast, duration);

        function closeToast() {
            clearTimeout(timeout);

            toast.classList.remove("show");

            setTimeout(() => {
                toast.remove();
            }, 300);
        }
        toast.addEventListener("click", closeToast);
    };

     window.showAchievementToast = function (achievement) {
    showToast(`
        🏆 Osiągnięcie odblokowane!<br>
        <strong>${achievement.name}</strong>
    `, 5000);
};
})();
