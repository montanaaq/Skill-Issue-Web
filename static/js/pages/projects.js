import { getProfile } from "../api/auth-api.js";
import {
    explainProjectMatch,
    getProjectRecommendations,
    getTeamCatalog,
    searchProjects,
} from "../api/teams-api.js";
import { errorMessages } from "@shared/constants/error-messages.js";
import { createLink, textElement } from "@shared/dom.js";
import {
    loadProjectSkillFilters,
    saveProjectSkillFilters,
} from "@shared/project-skill-filters.js";
import { clearError, showError } from "@shared/ui.js";
import { openApplicationDialog } from "./home/components/application-dialog.js";
import { initCreateProject } from "./home/components/create-project/create-project-dialog.js";
import { createSkillPicker } from "@shared/skill-picker.js";

const form = document.getElementById("project-filters");
const fields = form.querySelector("fieldset");
const pickerRoot = document.getElementById("project-skill-picker");
const results = document.getElementById("project-results");
const heading = document.getElementById("teams-heading");
const status = document.getElementById("projects-status");
const error = document.getElementById("projects-error");
const pages = document.getElementById("project-pages");
const firstPage = document.getElementById("first-project-page");
const nextPage = document.getElementById("next-project-page");
const findMatches = document.getElementById("find-project-matches");
const backToSearch = document.getElementById("back-to-project-search");
const matchTemplate = document.getElementById("project-match-template");
let appliedSkills = [];
let currentBefore = null;
let nextBefore = null;
let requestVersion = 0;
let showingMatches = false;
let picker;
let catalogRequest;

function ensureCatalog() {
    catalogRequest ??= getTeamCatalog().catch((cause) => {
        catalogRequest = undefined;
        throw cause;
    });
    return catalogRequest;
}

async function loadProjects(skillIds, before = null, focusHeading = false) {
    const loadMatches = skillIds === null;
    const version = ++requestVersion;
    clearError(error);
    firstPage.disabled = true;
    nextPage.disabled = true;
    if (loadMatches) {
        findMatches.setAttribute("aria-disabled", "true");
    } else {
        findMatches?.removeAttribute("aria-disabled");
    }
    status.classList.remove("visually-hidden");
    status.textContent = loadMatches
        ? "Finding team matches…"
        : "Loading projects…";

    try {
        const data = await (loadMatches
            ? getProjectRecommendations()
            : searchProjects(skillIds, before));
        if (version !== requestVersion) {
            return;
        }

        showingMatches = loadMatches;
        heading.textContent = loadMatches
            ? "Top teams for you"
            : "Projects looking for people";
        if (findMatches) {
            findMatches.hidden = loadMatches;
            backToSearch.hidden = !loadMatches;
        }

        if (loadMatches) {
            results.replaceChildren(...data.projects.map(renderMatch));
            if (!data.hasSkills) {
                results.append(createLink("Edit profile", "/profile"));
            }
            pages.hidden = true;
        } else {
            results.innerHTML = data.html;
            for (const button of results.querySelectorAll(
                "[data-apply-team]",
            )) {
                button.hidden = false;
            }
            appliedSkills = skillIds;
            currentBefore = before;
            nextBefore = data.nextBefore;
            firstPage.hidden = before === null;
            nextPage.hidden = nextBefore === null;
            pages.hidden = firstPage.hidden && nextPage.hidden;
        }

        const count = results.querySelectorAll("article").length;
        status.classList.toggle("visually-hidden", !loadMatches && count === 0);
        if (loadMatches) {
            if (!data.hasSkills) {
                status.textContent =
                    "Add skills to your profile to find matching teams.";
            } else {
                status.textContent = count
                    ? `Matching projects: ${count}.`
                    : "No open positions match your skills yet.";
            }
        } else {
            status.textContent = count
                ? `Projects on this page: ${count}.`
                : "No projects found.";
        }
        if (focusHeading) {
            heading.focus();
        }
    } catch (cause) {
        if (version === requestVersion) {
            status.textContent = "";
            showError(error, cause.message || errorMessages.requestFailed);
        }
    } finally {
        if (version === requestVersion) {
            firstPage.disabled = false;
            nextPage.disabled = false;
            findMatches?.removeAttribute("aria-disabled");
        }
    }
}

function renderMatch(project) {
    const card = matchTemplate.content.firstElementChild.cloneNode(true);
    const title = card.querySelector("h3");
    title.id = `team-heading-${project.teamId}`;
    title.textContent = project.teamName;
    card.querySelector(".team-description").textContent = project.description;
    const position = card.querySelector("[data-match-position]");
    position.id = `match-position-${project.positionId}`;
    position.textContent = project.positionTitle;
    card.querySelector("[data-match-score]").textContent =
        `${project.percent}% match`;

    const skills = card.querySelector("table");
    skills.setAttribute("aria-labelledby", position.id);
    const body = skills.tBodies[0];
    for (const { name, candidateLevel, requiredLevel } of project.skills) {
        const row = body.insertRow();
        const skill = textElement("th", name);
        skill.scope = "row";
        const status =
            candidateLevel === null
                ? "Missing"
                : candidateLevel >= requiredLevel
                  ? "Meets requirement"
                  : "Below required level";
        skill.append(textElement("small", status, "project-skill-status"));
        row.append(
            skill,
            textElement(
                "td",
                candidateLevel === null ? "—" : `${candidateLevel}/3`,
            ),
            textElement("td", `${requiredLevel}/3`),
        );
    }

    const apply = card.querySelector("[data-apply-team]");
    apply.setAttribute("aria-describedby", title.id);
    apply.dataset.applyTeam = project.teamId;
    apply.dataset.positions = JSON.stringify([
        {
            id: project.positionId,
            title: project.positionTitle,
            category: project.positionCategory,
            skills: project.skills.map((skill) => skill.name),
        },
    ]);
    const explain = card.querySelector("[data-explain-match]");
    explain.setAttribute("aria-describedby", title.id);
    explain.dataset.explainMatch = project.positionId;

    return card;
}

async function explainMatch(button) {
    if (button.getAttribute("aria-disabled") === "true") {
        return;
    }

    const explanation = button
        .closest("article")
        .querySelector("[data-match-explanation]");
    button.setAttribute("aria-disabled", "true");
    button.textContent = "Explaining…";
    explanation.textContent = "";

    try {
        explanation.textContent = await explainProjectMatch(
            Number(button.dataset.explainMatch),
        );
        button.textContent = "Match explained";
    } catch (cause) {
        explanation.textContent = cause.message || errorMessages.requestFailed;
        button.textContent = "Explain match (AI)";
        button.removeAttribute("aria-disabled");
    }
}

function applyFilters() {
    const skillIds = picker.ids;
    saveProjectSkillFilters(skillIds);
    return loadProjects(skillIds);
}

async function initFilters() {
    status.textContent = "Loading projects…";

    try {
        const savedIds = loadProjectSkillFilters();
        const profileRequest =
            savedIds === null && results.dataset.signedIn === "True"
                ? getProfile()
                : null;
        const [catalog, profile] = await Promise.all([
            ensureCatalog(),
            profileRequest,
        ]);
        picker = createSkillPicker(
            pickerRoot,
            catalog.skills,
            () => {},
            savedIds ?? profile?.skillIds ?? [],
        );
        fields.disabled = false;
        if (findMatches) {
            findMatches.hidden = false;
        }
        await loadProjects(picker.ids);
    } catch (cause) {
        status.textContent = "";
        showError(error, cause.message || errorMessages.requestFailed);
    }
}

form.addEventListener("submit", (event) => {
    event.preventDefault();
    if (picker) {
        applyFilters();
    }
});

document
    .getElementById("reset-project-filters")
    .addEventListener("click", () => {
        picker.clear();
        pickerRoot.querySelector("input").focus();
        applyFilters();
    });

firstPage.addEventListener("click", () =>
    loadProjects(appliedSkills, null, true),
);
nextPage.addEventListener("click", () =>
    loadProjects(appliedSkills, nextBefore, true),
);
findMatches?.addEventListener("click", () => {
    if (findMatches.getAttribute("aria-disabled") !== "true") {
        loadProjects(null, null, true);
    }
});
backToSearch?.addEventListener("click", () =>
    loadProjects(appliedSkills, null, true),
);

results.addEventListener("click", (event) => {
    const explain = event.target.closest("[data-explain-match]");
    if (explain) {
        explainMatch(explain);
        return;
    }

    const button = event.target.closest("[data-apply-team]");
    if (!button) {
        return;
    }

    const projectHeading = document.getElementById(
        button.getAttribute("aria-describedby"),
    );
    const team = {
        id: Number(button.dataset.applyTeam),
        name: projectHeading.textContent,
        applicationPositions: JSON.parse(button.dataset.positions),
    };
    openApplicationDialog(team, button, () =>
        loadProjects(
            showingMatches ? null : appliedSkills,
            currentBefore,
            true,
        ),
    );
});

initCreateProject(ensureCatalog);
initFilters();
