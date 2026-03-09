import {
    badgeClass,
    escapeHtml,
    fetchJson,
    formatTemperature,
    formatTimestamp,
    formatWaveHeight,
    formatWindSpeed,
    markerClass,
} from "./shared.js";

const SVG_NS = "http://www.w3.org/2000/svg";
const AUTO_REFRESH_MS = 5 * 60 * 1000;
const MAP_VIEWBOX_WIDTH = 1000;
const MAP_VIEWBOX_HEIGHT = 620;
const MARKER_LABEL_OFFSET_X = 11;
const MARKER_LABEL_OFFSET_Y = -10;
const MAP_EDGE_PADDING = 10;
const LABEL_MAX_VISIBLE_LENGTH = 24;
const MAP_PAN_MARGIN_X = 140;
const MAP_PAN_MARGIN_Y = 90;
const MAP_DRAG_THRESHOLD_PX = 4;
const P31_WRECK_SITE_NAME = "P31 Wreck";

const state = {
    sites: [],
    weatherBySite: new Map(),
    selectedSiteId: null,
    lastRefreshUtc: null,
};

const mapState = {
    pointerId: null,
    dragStartClientX: 0,
    dragStartClientY: 0,
    dragStartViewBoxX: 0,
    dragStartViewBoxY: 0,
    dragDistancePx: 0,
    viewBoxX: 0,
    viewBoxY: 0,
    pressedSiteId: null,
    suppressNextMarkerClick: false,
};

const elements = {
    radarMap: document.getElementById("radarMap"),
    markerLayer: document.getElementById("markerLayer"),
    lastRefresh: document.getElementById("lastRefresh"),
    siteName: document.getElementById("siteName"),
    siteMeta: document.getElementById("siteMeta"),
    conditionBadge: document.getElementById("conditionBadge"),
    conditionSummary: document.getElementById("conditionSummary"),
    airTemp: document.getElementById("airTemp"),
    waterTemp: document.getElementById("waterTemp"),
    windSpeed: document.getElementById("windSpeed"),
    windDirection: document.getElementById("windDirection"),
    waveHeight: document.getElementById("waveHeight"),
    seaState: document.getElementById("seaState"),
    observationTime: document.getElementById("observationTime"),
    updatedTime: document.getElementById("updatedTime"),
    providerSource: document.getElementById("providerSource"),
    providerList: document.getElementById("providerList"),
};

window.addEventListener("DOMContentLoaded", () => {
    initializeMapInteractions();
    void initializeAsync();
});

async function initializeAsync() {
    await refreshDashboardAsync();

    window.setInterval(() => {
        void refreshDashboardAsync();
    }, AUTO_REFRESH_MS);
}

async function refreshDashboardAsync() {
    try {
        const [sites, latestWeather] = await Promise.all([
            fetchJson("/api/sites"),
            fetchJson("/api/weather/latest"),
        ]);

        state.sites = Array.isArray(sites)
            ? sites.filter((site) => site.isActive)
            : [];

        const snapshots = Array.isArray(latestWeather?.snapshots)
            ? latestWeather.snapshots
            : [];

        state.weatherBySite = new Map(
            snapshots.map((snapshot) => [snapshot.diveSiteId, snapshot]),
        );

        state.lastRefreshUtc = latestWeather?.lastRefreshUtc ?? null;

        if (
            state.selectedSiteId === null ||
            !state.sites.some((site) => site.id === state.selectedSiteId)
        ) {
            state.selectedSiteId = state.sites.length > 0
                ? state.sites[0].id
                : null;
        }

        renderHeader();
        renderMarkers();
        renderSitePanel();
    } catch (error) {
        const message = error instanceof Error ? error.message : "Unknown error";
        elements.lastRefresh.textContent = `Refresh failed (${message})`;
    }
}

function renderHeader() {
    if (state.lastRefreshUtc) {
        elements.lastRefresh.textContent = formatTimestamp(state.lastRefreshUtc);
        return;
    }

    const newestSnapshot = Array.from(state.weatherBySite.values())
        .map((snapshot) => snapshot.lastUpdatedUtc)
        .filter((value) => !!value)
        .sort()
        .at(-1);

    elements.lastRefresh.textContent = newestSnapshot
        ? formatTimestamp(newestSnapshot)
        : "No completed refresh yet";
}

function renderMarkers() {
    elements.markerLayer.replaceChildren();

    for (const site of state.sites) {
        const snapshot = state.weatherBySite.get(site.id);
        const marker = document.createElementNS(SVG_NS, "g");

        marker.classList.add("marker", markerClass(snapshot));
        if (site.id === state.selectedSiteId) {
            marker.classList.add("active");
        }

        marker.setAttribute("transform", `translate(${site.displayX} ${site.displayY})`);
        marker.setAttribute("role", "button");
        marker.setAttribute("tabindex", "0");
        marker.setAttribute("aria-label", `Select ${site.name}`);
        marker.dataset.siteId = String(site.id);

        marker.addEventListener("click", () => {
            if (consumeSuppressedMarkerClick()) {
                return;
            }

            selectSite(site.id);
        });
        marker.addEventListener("keydown", (event) => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                selectSite(site.id);
            }
        });

        const title = document.createElementNS(SVG_NS, "title");
        title.textContent = `${site.name} - ${snapshot?.conditionStatus ?? "No Data"}`;
        marker.appendChild(title);

        marker.appendChild(createLine(-12, 0, 12, 0));
        marker.appendChild(createLine(0, -12, 0, 12));

        const dot = document.createElementNS(SVG_NS, "circle");
        dot.setAttribute("r", "7");
        marker.appendChild(dot);

        const label = document.createElementNS(SVG_NS, "text");
        const labelText = shortLabel(site.name);
        const customLabelPosition = getCustomMarkerLabelPosition(site);

        if (customLabelPosition) {
            label.setAttribute("x", String(customLabelPosition.x));
            label.setAttribute("y", String(customLabelPosition.y));
            label.setAttribute("text-anchor", customLabelPosition.anchor);
        } else {
            label.setAttribute("x", String(MARKER_LABEL_OFFSET_X));
            label.setAttribute("y", String(MARKER_LABEL_OFFSET_Y));
            label.setAttribute("text-anchor", "start");
        }

        label.textContent = labelText;
        marker.appendChild(label);

        elements.markerLayer.appendChild(marker);

        if (!customLabelPosition) {
            positionMarkerLabel(site, label);
        }
    }
}

function renderSitePanel() {
    const site = state.sites.find((item) => item.id === state.selectedSiteId);

    if (!site) {
        resetPanel("Select a dive site", "Awaiting marker selection");
        return;
    }

    const snapshot = state.weatherBySite.get(site.id);

    elements.siteName.textContent = site.name;
    elements.siteMeta.textContent = `${site.island}  | ${site.latitude.toFixed(4)}, ${site.longitude.toFixed(4)}`;

    elements.conditionBadge.className = `condition-badge ${badgeClass(snapshot)}`;
    elements.conditionBadge.textContent = snapshot
        ? `${snapshot.conditionStatus}${snapshot.isStale ? " (Stale)" : ""}`
        : "No Data";

    elements.conditionSummary.textContent = snapshot?.conditionSummary ??
        "No weather snapshot available for this site.";

    elements.airTemp.textContent = formatTemperature(snapshot?.airTemperatureC);
    elements.waterTemp.textContent = formatTemperature(snapshot?.waterTemperatureC);
    elements.windSpeed.textContent = formatWindSpeed(snapshot?.windSpeedMps);
    elements.windDirection.textContent = formatWindDirection(snapshot);
    elements.waveHeight.textContent = formatWaveHeight(snapshot?.waveHeightM);
    elements.seaState.textContent = snapshot?.seaStateText ?? "--";
    elements.observationTime.textContent = formatTimestamp(snapshot?.observationTimeUtc);
    elements.updatedTime.textContent = formatTimestamp(snapshot?.lastUpdatedUtc);
    elements.providerSource.textContent = snapshot?.sourceProvider ?? "--";

    renderProviderList(snapshot?.providerSnapshots ?? []);
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

function selectSite(siteId) {
    state.selectedSiteId = siteId;
    renderMarkers();
    renderSitePanel();
}

function resetPanel(siteName, siteMeta) {
    elements.siteName.textContent = siteName;
    elements.siteMeta.textContent = siteMeta;
    elements.conditionBadge.className = "condition-badge condition-unknown";
    elements.conditionBadge.textContent = "No Data";
    elements.conditionSummary.textContent =
        "Choose a marker to inspect latest conditions.";

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

function createLine(x1, y1, x2, y2) {
    const line = document.createElementNS(SVG_NS, "line");
    line.setAttribute("x1", String(x1));
    line.setAttribute("y1", String(y1));
    line.setAttribute("x2", String(x2));
    line.setAttribute("y2", String(y2));
    return line;
}

function shortLabel(value) {
    if (value.length <= LABEL_MAX_VISIBLE_LENGTH) {
        return value;
    }

    return `${value.slice(0, LABEL_MAX_VISIBLE_LENGTH - 3)}...`;
}

function getCustomMarkerLabelPosition(site) {
    if (site.name === P31_WRECK_SITE_NAME) {
        return {
            x: -MARKER_LABEL_OFFSET_X,
            y: 16,
            anchor: "end",
        };
    }

    return null;
}

function positionMarkerLabel(site, label) {
    const labelWidth = label.getComputedTextLength();
    const minVisibleX = MAP_EDGE_PADDING;
    const maxVisibleX = MAP_VIEWBOX_WIDTH - MAP_EDGE_PADDING;

    const rightStart = site.displayX + MARKER_LABEL_OFFSET_X;
    const rightEnd = rightStart + labelWidth;
    const leftEnd = site.displayX - MARKER_LABEL_OFFSET_X;
    const leftStart = leftEnd - labelWidth;

    const rightOverflow = Math.max(0, rightEnd - maxVisibleX);
    const leftOverflow = Math.max(0, minVisibleX - leftStart);

    if (rightOverflow <= leftOverflow) {
        label.setAttribute("x", String(MARKER_LABEL_OFFSET_X));
        label.setAttribute("text-anchor", "start");
        return;
    }

    label.setAttribute("x", String(-MARKER_LABEL_OFFSET_X));
    label.setAttribute("text-anchor", "end");
}

function initializeMapInteractions() {
    if (!elements.radarMap) {
        return;
    }

    mapState.viewBoxX = 0;
    mapState.viewBoxY = 0;

    elements.radarMap.addEventListener("pointerdown", handleMapPointerDown);
    elements.radarMap.addEventListener("pointermove", handleMapPointerMove);
    elements.radarMap.addEventListener("pointerup", handleMapPointerUp);
    elements.radarMap.addEventListener("pointercancel", handleMapPointerUp);
    elements.radarMap.addEventListener("lostpointercapture", handleMapPointerUp);
    elements.radarMap.addEventListener("dblclick", () => {
        resetMapViewBox();
    });
}

function handleMapPointerDown(event) {
    if (!elements.radarMap || event.button !== 0) {
        return;
    }

    mapState.pointerId = event.pointerId;
    mapState.dragStartClientX = event.clientX;
    mapState.dragStartClientY = event.clientY;
    mapState.dragStartViewBoxX = mapState.viewBoxX;
    mapState.dragStartViewBoxY = mapState.viewBoxY;
    mapState.dragDistancePx = 0;
    mapState.pressedSiteId = getSiteIdFromEventTarget(event.target);

    elements.radarMap.setPointerCapture(event.pointerId);
    elements.radarMap.classList.add("is-dragging");
}

function handleMapPointerMove(event) {
    if (!elements.radarMap || mapState.pointerId !== event.pointerId) {
        return;
    }

    const deltaX = event.clientX - mapState.dragStartClientX;
    const deltaY = event.clientY - mapState.dragStartClientY;

    mapState.dragDistancePx = Math.max(
        mapState.dragDistancePx,
        Math.hypot(deltaX, deltaY),
    );

    if (mapState.dragDistancePx < MAP_DRAG_THRESHOLD_PX) {
        return;
    }

    const pixelsToViewBoxX = MAP_VIEWBOX_WIDTH / elements.radarMap.clientWidth;
    const pixelsToViewBoxY = MAP_VIEWBOX_HEIGHT / elements.radarMap.clientHeight;

    const nextX = clamp(
        mapState.dragStartViewBoxX - (deltaX * pixelsToViewBoxX),
        -MAP_PAN_MARGIN_X,
        MAP_PAN_MARGIN_X,
    );

    const nextY = clamp(
        mapState.dragStartViewBoxY - (deltaY * pixelsToViewBoxY),
        -MAP_PAN_MARGIN_Y,
        MAP_PAN_MARGIN_Y,
    );

    setMapViewBox(nextX, nextY);
}

function handleMapPointerUp(event) {
    if (!elements.radarMap || mapState.pointerId !== event.pointerId) {
        return;
    }

    const wasDrag = mapState.dragDistancePx >= MAP_DRAG_THRESHOLD_PX;

    if (wasDrag) {
        mapState.suppressNextMarkerClick = true;
    } else if (mapState.pressedSiteId !== null) {
        selectSite(mapState.pressedSiteId);
        mapState.suppressNextMarkerClick = true;
    }

    if (elements.radarMap.hasPointerCapture(event.pointerId)) {
        elements.radarMap.releasePointerCapture(event.pointerId);
    }

    mapState.pointerId = null;
    mapState.dragDistancePx = 0;
    mapState.pressedSiteId = null;
    elements.radarMap.classList.remove("is-dragging");
}

function resetMapViewBox() {
    setMapViewBox(0, 0);
}

function setMapViewBox(x, y) {
    if (!elements.radarMap) {
        return;
    }

    mapState.viewBoxX = x;
    mapState.viewBoxY = y;

    elements.radarMap.setAttribute(
        "viewBox",
        `${x} ${y} ${MAP_VIEWBOX_WIDTH} ${MAP_VIEWBOX_HEIGHT}`,
    );
}

function consumeSuppressedMarkerClick() {
    if (!mapState.suppressNextMarkerClick) {
        return false;
    }

    mapState.suppressNextMarkerClick = false;
    return true;
}

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function getSiteIdFromEventTarget(target) {
    if (!(target instanceof Element)) {
        return null;
    }

    const marker = target.closest("g.marker");
    if (!marker) {
        return null;
    }

    const siteIdRaw = marker.getAttribute("data-site-id");
    if (!siteIdRaw) {
        return null;
    }

    const siteId = Number(siteIdRaw);
    return Number.isInteger(siteId) ? siteId : null;
}

function formatWindDirection(snapshot) {
    if (!snapshot || snapshot.windDirectionDeg === null || snapshot.windDirectionDeg === undefined) {
        return "--";
    }

    const cardinal = snapshot.windDirectionCardinal ?? "";
    return `${snapshot.windDirectionDeg} deg ${cardinal}`.trim();
}
