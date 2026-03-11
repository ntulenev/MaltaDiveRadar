import {
    badgeClass,
    escapeHtml,
    fetchJson,
    formatTemperature,
    formatTimestamp,
    formatWaveHeight,
    formatWindSpeed,
} from "./shared.js?v=20260309.2";

const elements = {
    lastRefresh: document.getElementById("detailsLastRefresh"),
    siteName: document.getElementById("detailsSiteName"),
    siteMeta: document.getElementById("detailsSiteMeta"),
    siteDescription: document.getElementById("detailsSiteDescription"),
    snapshotMeta: document.getElementById("detailsSnapshotMeta"),
    generalWeather: document.getElementById("detailsGeneralWeather"),
    conditionBadge: document.getElementById("detailsConditionBadge"),
    conditionSummary: document.getElementById("detailsConditionSummary"),
    airTemp: document.getElementById("detailsAirTemp"),
    waterTemp: document.getElementById("detailsWaterTemp"),
    windSpeed: document.getElementById("detailsWindSpeed"),
    windDirection: document.getElementById("detailsWindDirection"),
    waveHeight: document.getElementById("detailsWaveHeight"),
    seaState: document.getElementById("detailsSeaState"),
    observationTime: document.getElementById("detailsObservationTime"),
    updatedTime: document.getElementById("detailsUpdatedTime"),
    providerSource: document.getElementById("detailsProviderSource"),
    providerList: document.getElementById("detailsProviderList"),
};

window.addEventListener("DOMContentLoaded", () => {
    void initializeAsync();
});

async function initializeAsync() {
    const siteId = parseSiteIdFromQuery();

    if (siteId === null) {
        renderPageError("Invalid or missing site id.");
        return;
    }

    try {
        const [site, weather] = await Promise.all([
            fetchJson(`/api/sites/${siteId}`),
            fetchOptionalSiteWeatherAsync(siteId),
        ]);

        renderSite(site);
        renderWeather(weather);
    } catch (error) {
        const message = error instanceof Error ? error.message : "Unknown error";
        renderPageError(message);
    }
}

async function fetchOptionalSiteWeatherAsync(siteId) {
    const response = await fetch(`/api/sites/${siteId}/weather`, {
        method: "GET",
        headers: {
            Accept: "application/json",
        },
        cache: "no-store",
    });

    if (response.status === 404) {
        return null;
    }

    if (response.ok) {
        return await response.json();
    }

    const message = await tryReadErrorMessageAsync(response);
    throw new Error(message);
}

async function tryReadErrorMessageAsync(response) {
    let message = `Request failed: ${response.status}`;

    try {
        const body = await response.json();
        if (typeof body?.error === "string" && body.error.length > 0) {
            message = body.error;
        }
    } catch {
        // Ignore JSON parsing failures for error payloads.
    }

    return message;
}

function parseSiteIdFromQuery() {
    const siteIdRaw = new URLSearchParams(window.location.search).get("id");
    const siteId = Number(siteIdRaw);

    if (!Number.isInteger(siteId) || siteId <= 0) {
        return null;
    }

    return siteId;
}

function renderSite(site) {
    elements.siteName.textContent = site.name;
    elements.siteMeta.textContent =
        `${site.island}  | ${site.latitude.toFixed(4)}, ${site.longitude.toFixed(4)}`;

    const description = typeof site.description === "string"
        ? site.description.trim()
        : "";
    elements.siteDescription.textContent = description.length > 0
        ? description
        : "No site description available.";
}

function renderWeather(snapshot) {
    if (!snapshot) {
        renderNoWeatherState();
        return;
    }

    elements.lastRefresh.textContent = formatTimestamp(snapshot.lastUpdatedUtc);
    elements.snapshotMeta.textContent = "Latest weather snapshot";
    elements.generalWeather.textContent = formatGeneralWeather(snapshot);
    elements.conditionBadge.className = `condition-badge ${badgeClass(snapshot)}`;
    elements.conditionBadge.textContent = snapshot.isStale
        ? `${snapshot.conditionStatus} (Stale)`
        : snapshot.conditionStatus;
    elements.conditionSummary.textContent =
        snapshot.conditionSummary ?? "No summary provided.";

    elements.airTemp.textContent = formatTemperature(snapshot.airTemperatureC);
    elements.waterTemp.textContent = formatTemperature(snapshot.waterTemperatureC);
    elements.windSpeed.textContent = formatWindSpeed(snapshot.windSpeedMps);
    elements.windDirection.textContent = formatWindDirection(snapshot);
    elements.waveHeight.textContent = formatWaveHeight(snapshot.waveHeightM);
    elements.seaState.textContent = snapshot.seaStateText ?? "--";
    elements.observationTime.textContent = formatTimestamp(snapshot.observationTimeUtc);
    elements.updatedTime.textContent = formatTimestamp(snapshot.lastUpdatedUtc);
    elements.providerSource.textContent = snapshot.sourceProvider ?? "--";

    renderProviderList(snapshot.providerSnapshots ?? []);
}

function renderNoWeatherState() {
    elements.lastRefresh.textContent = "--";
    elements.snapshotMeta.textContent = "No weather snapshot is available yet";
    elements.generalWeather.textContent = "--";
    elements.conditionBadge.className = "condition-badge condition-unknown";
    elements.conditionBadge.textContent = "No Data";
    elements.conditionSummary.textContent =
        "Weather data for this site is not available yet.";

    elements.airTemp.textContent = "--";
    elements.waterTemp.textContent = "--";
    elements.windSpeed.textContent = "--";
    elements.windDirection.textContent = "--";
    elements.waveHeight.textContent = "--";
    elements.seaState.textContent = "--";
    elements.observationTime.textContent = "--";
    elements.updatedTime.textContent = "--";
    elements.providerSource.textContent = "--";

    renderProviderList([]);
}

function renderProviderList(providerSnapshots) {
    elements.providerList.replaceChildren();

    if (!Array.isArray(providerSnapshots) || providerSnapshots.length === 0) {
        const item = document.createElement("li");
        item.textContent = "No provider diagnostics available yet.";
        elements.providerList.appendChild(item);
        return;
    }

    for (const provider of providerSnapshots) {
        const item = document.createElement("li");
        const statusClass = provider.isSuccess ? "provider-ok" : "provider-failed";
        const quality = typeof provider.qualityScore === "number"
            ? provider.qualityScore.toFixed(2)
            : "--";

        const detail =
            `<strong>${escapeHtml(provider.providerName)}</strong> | ` +
            `<span class="${statusClass}">${provider.isSuccess ? "OK" : "FAILED"}</span> | ` +
            `Q ${quality} | ${formatTimestamp(provider.retrievedAtUtc)}`;

        const errorSuffix = provider.error
            ? `<br><span class="provider-failed">${escapeHtml(provider.error)}</span>`
            : "";

        item.innerHTML = `${detail}${errorSuffix}`;
        elements.providerList.appendChild(item);
    }
}

function renderPageError(message) {
    elements.siteName.textContent = "Unable to load dive site";
    elements.siteMeta.textContent = "Request failed";
    elements.siteDescription.textContent = message;
    renderNoWeatherState();
}

function formatWindDirection(snapshot) {
    if (!snapshot || snapshot.windDirectionDeg === null || snapshot.windDirectionDeg === undefined) {
        return "--";
    }

    const cardinal = snapshot.windDirectionCardinal ?? "";
    return `${snapshot.windDirectionDeg} deg ${cardinal}`.trim();
}

function formatGeneralWeather(snapshot) {
    if (!snapshot) {
        return "--";
    }

    const generalWeatherText = typeof snapshot.generalWeatherText === "string"
        ? snapshot.generalWeatherText.trim()
        : "";

    if (generalWeatherText.length > 0) {
        return generalWeatherText;
    }

    const status = String(snapshot.conditionStatus ?? "").toLowerCase();
    if (status === "good") {
        return "Fair";
    }

    if (status === "caution") {
        return "Variable";
    }

    if (status === "rough") {
        return "Unsettled";
    }

    return "--";
}
