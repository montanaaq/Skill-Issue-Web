import {
    getCurrentUser,
    getProfile,
    saveSkills,
    signOut,
} from "../../api/auth-api.js";
import { getTeamCatalog } from "../../api/teams-api.js";
import { errorMessages } from "@shared/constants/error-messages.js";
import { clearError, showError } from "@shared/ui.js";
import { initAccent } from "./accent.js";
import { $ } from "@shared/dom.js";
import { createSkillPicker } from "@shared/skill-picker.js";
import { renderApplications } from "./render.js";
import { initSocialLinks, renderLinks } from "./social-links.js";

const errorSummary = document.getElementById("account-error");
const logout = $("signout");
const retry = $("retry");
const loading = $("loading");

function selectSection() {
    const selected =
        window.location.hash === "#settings" ? "settings" : "overview";
    for (const section of document.querySelectorAll("[data-section]")) {
        section.hidden = section.dataset.section !== selected;
    }
    for (const link of document.querySelectorAll("[data-section-link]")) {
        if (link.dataset.sectionLink === selected) {
            link.setAttribute("aria-current", "page");
        } else {
            link.removeAttribute("aria-current");
        }
    }
}

function renderSkillNames(names) {
    const skills = $("profile-skills");
    skills.textContent = names.join(" · ");
    skills.hidden = names.length === 0;
}

function initProfileSkills(profile, skills) {
    const form = $("skills-form");
    const fields = $("skills-fields");
    const status = $("skills-status");
    const error = $("skills-error");
    const byId = new Map(skills.map(({ id, name }) => [id, name]));
    const editableNames = new Set(profile.skillIds.map((id) => byId.get(id)));
    const inactiveNames = profile.skills.filter(
        (name) => !editableNames.has(name),
    );
    let saved = profile.editableSkills;
    let levels = new Map(saved.map(({ skillId, skillLevel }) => [skillId, skillLevel]));

    function clearFeedback() {
        clearError(error);
        status.textContent = "";
    }

    const picker = createSkillPicker(
        $("profile-skill-picker"),
        skills,
        (ids) => {
            levels = new Map(ids.map((id) => [id, levels.get(id) ?? 1]));
            clearFeedback();
        },
        profile.skillIds,
        ({ id, name }, createSkillSelect) => [
            createSkillSelect(
                `profile-skill-${id}-level`,
                `Level for ${name}`,
                levels.get(id),
                (value) => {
                    levels.set(id, value);
                    clearFeedback();
                },
            ),
        ],
    );

    $("cancel-skills").addEventListener("click", () => {
        levels = new Map(saved.map(({ skillId, skillLevel }) => [skillId, skillLevel]));
        picker.reset([...levels.keys()]);
        clearFeedback();
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        if (fields.disabled) {
            return;
        }

        fields.disabled = true;
        clearFeedback();
        const ids = picker.ids;
        const selection = ids.map((skillId) => ({ skillId, skillLevel: levels.get(skillId) }));
        try {
            await saveSkills(selection);
            saved = selection;
            renderSkillNames(
                [...inactiveNames, ...ids.map((id) => byId.get(id))].sort(),
            );
            status.textContent = "Skills saved.";
        } catch (cause) {
            showError(error, cause.message || errorMessages.requestFailed);
        } finally {
            fields.disabled = false;
        }
    });
}

function renderProfile(user, profile, catalog) {
    $("profile-name").textContent = user.fullName;
    $("profile-email").textContent = user.email;

    renderSkillNames(profile.skills);
    initProfileSkills(profile, catalog.skills);
    renderLinks(profile.links);
    renderApplications(profile.applications);
    $("profile").hidden = false;
}

async function loadProfile() {
    retry.hidden = true;
    loading.hidden = false;
    clearError(errorSummary);
    try {
        const user = await getCurrentUser();
        if (user === null) {
            window.location.replace("/signin");
            return;
        }
        logout.disabled = false;
        $("profile-role").textContent = user.role;
        const [profile, catalog] = await Promise.all([
            getProfile(),
            getTeamCatalog(),
        ]);
        renderProfile(user, profile, catalog);
    } catch {
        showError(errorSummary, errorMessages.requestFailed);
        retry.hidden = false;
    } finally {
        loading.hidden = true;
    }
}

async function handleLogout() {
    logout.disabled = true;
    clearError(errorSummary);
    try {
        await signOut();
        window.location.replace("/");
    } catch {
        showError(errorSummary, errorMessages.requestFailed);
        logout.disabled = false;
    }
}

logout.addEventListener("click", handleLogout);
retry.addEventListener("click", loadProfile);
window.addEventListener("hashchange", selectSection);

initSocialLinks();
initAccent();
selectSection();
loadProfile();
