import { getAdminCatalog, setCatalogItemActive } from "../api/admin-api.js";
import { $, textElement } from "@shared/dom.js";
import { clearError, showError } from "@shared/ui.js";

const catalog = $("catalog");
const loading = $("loading");
const loadError = $("load-error");
const retry = $("retry");
const rowTemplate = $("row-template");

function createRow({ kind, id, name, isActive }) {
    const row = rowTemplate.content.cloneNode(true);
    const status = $("status", row);
    const toggle = $("toggle", row);
    const error = $("error", row);

    function renderState() {
        const action = isActive ? "Disable" : "Enable";
        status.textContent = `${name} (${isActive ? "enabled" : "disabled"})`;
        toggle.textContent = action;
        toggle.setAttribute("aria-label", `${action} ${name}`);
    }

    toggle.addEventListener("click", async () => {
        toggle.disabled = true;
        clearError(error);

        try {
            await setCatalogItemActive(kind, id, !isActive);
            isActive = !isActive;
            renderState();
        } catch (cause) {
            showError(error, cause.message);
        } finally {
            toggle.disabled = false;
        }
    });

    renderState();
    return row;
}

async function loadCatalog() {
    loading.hidden = false;
    retry.hidden = true;
    clearError(loadError);

    try {
        const items = await getAdminCatalog();
        for (const kind of ["position-categories", "skills"]) {
            const rows = items
                .filter((item) => item.kind === kind)
                .map(createRow);
            $(kind).replaceChildren(
                ...(rows.length ? rows : [textElement("li", "No entries.")]),
            );
        }

        catalog.hidden = false;
    } catch (cause) {
        showError(loadError, cause.message);
        retry.hidden = false;
    } finally {
        loading.hidden = true;
    }
}

retry.addEventListener("click", loadCatalog);
loadCatalog();
