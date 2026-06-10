window.authFetch = async function (url, options = {}) {
    options.credentials = "include";

    options.headers = options.headers || {};

    if (options.body instanceof FormData) {
        delete options.headers["Content-Type"];
    } else if (options.body && typeof options.body === "string") {
        options.headers["Content-Type"] = "application/json";
    }

    let res = await fetch(url, options);

    if (res.status === 401) {
        const refreshRes = await fetch("/api/refresh", { method: "POST", credentials: "include" });
        if (!refreshRes.ok) {
            window.location.href = "index.html";
            return;
        }
        res = await fetch(url, options);
    }

    return res;
};