export async function fetchJson(url, signal) {
    const response = await fetch(url, {
        method: "GET",
        headers: {
            Accept: "application/json",
        },
        cache: "no-store",
        signal,
    });

    if (response.ok) {
        return await response.json();
    }

    let message = `Request failed: ${response.status}`;
    try {
        const body = await response.json();
        if (typeof body?.error === "string" && body.error.length > 0) {
            message = body.error;
        }
    } catch {
        // Ignore JSON parsing failures for error payloads.
    }

    throw new Error(message);
}

export function formatTemperature(value) {
    if (value === null || value === undefined) {
        return "--";
    }

    return `${value.toFixed(1)} deg C`;
}

export function formatWindSpeed(value) {
    if (value === null || value === undefined) {
        return "--";
    }

    return `${value.toFixed(1)} m/s`;
}

export function formatWaveHeight(value) {
    if (value === null || value === undefined) {
        return "--";
    }

    return `${value.toFixed(2)} m`;
}

export function formatTimestamp(value) {
    if (!value) {
        return "--";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return "--";
    }

    const formatted = parsed.toLocaleString("en-MT", {
        year: "numeric",
        month: "short",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
        timeZone: "Europe/Malta",
        timeZoneName: "short",
    });

    return formatted;
}

export function markerClass(snapshot) {
    if (!snapshot || snapshot.isStale) {
        return "marker--stale";
    }

    const status = String(snapshot.conditionStatus ?? "").toLowerCase();
    if (status === "good") {
        return "marker--good";
    }

    if (status === "caution") {
        return "marker--caution";
    }

    if (status === "rough") {
        return "marker--rough";
    }

    return "marker--stale";
}

export function badgeClass(snapshot) {
    if (!snapshot || snapshot.isStale) {
        return "condition-unknown";
    }

    const status = String(snapshot.conditionStatus ?? "").toLowerCase();
    if (status === "good") {
        return "condition-good";
    }

    if (status === "caution") {
        return "condition-caution";
    }

    if (status === "rough") {
        return "condition-rough";
    }

    return "condition-unknown";
}

export function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}
