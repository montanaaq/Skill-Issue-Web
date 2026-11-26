import {
    ACCENT_PRESETS,
    ACCENT_STORAGE_KEY,
    DIM_ACCENT_BRIGHTNESS,
} from "@shared/constants/accent.js";
import { applyAccent } from "@shared/accent.js";
import { $, textElement } from "@shared/dom.js";

export function initAccent() {
    const picker = $("accent-presets");
    const customColor = $("custom-accent");
    const status = $("accent-status");
    const root = document.documentElement;

    function updatePicker() {
        customColor.value = root.dataset.accentColor;
        for (const radio of picker.querySelectorAll("input")) {
            radio.checked = radio.value === root.dataset.accent;
        }
        const [red, green, blue] = customColor.value
            .match(/\w\w/g)
            .map((value) => parseInt(value, 16));
        const brightness = (red * 299 + green * 587 + blue * 114) / 1000;
        $("accent-warning").hidden = brightness >= DIM_ACCENT_BRIGHTNESS;
    }

    function selectAccent(value) {
        applyAccent(value);
        updatePicker();
        try {
            localStorage.setItem(ACCENT_STORAGE_KEY, value);
            status.textContent = "Accent saved.";
        } catch {
            status.textContent =
                "Accent applied. Browser storage is unavailable.";
        }
    }

    for (const { name, label } of ACCENT_PRESETS) {
        const option = document.createElement("label");
        const radio = document.createElement("input");
        radio.type = "radio";
        radio.name = "accent";
        radio.value = name;
        radio.addEventListener("change", () => selectAccent(name));
        option.append(radio, textElement("span", label));
        picker.append(option);
    }
    customColor.addEventListener("input", () =>
        selectAccent(customColor.value),
    );
    updatePicker();
}
