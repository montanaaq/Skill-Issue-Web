import { suggestTeam } from "../../../../api/teams-api.js";
import { errorMessages } from "@shared/constants/error-messages.js";
import { MAX_PEOPLE } from "@shared/constants/limits.js";
import { clearError, showError } from "@shared/ui.js";
import {
    createPosition,
    totalPlaces,
    hasCaptain,
    hasDraftPositions,
    formatPlaces,
    toggleCaptain,
} from "./draft.js";
import {
    isCurrentProject,
    reportFirstInvalid,
    updateCharacterCount,
} from "./form-fields.js";

export function createPositionsStep(section, draft, catalog, editDescription) {
    const people = section.querySelector("#project-positions");
    const add = section.querySelector("#add-project-position");
    const suggest = section.querySelector("#suggest-project-team");
    const suggestionStatus = section.querySelector(
        "#project-suggestion-status",
    );
    const suggestionError = section.querySelector("#project-suggestion-error");
    const edit = section.querySelector("#edit-description-for-suggestion");
    const countLabel = section.querySelector("#project-people-count");
    const peopleError = section.querySelector("#project-people-error");
    const template = document.getElementById("project-position-template");
    const form = section.closest("form");
    const dialog = section.closest("dialog");
    let pending = false;
    let suggestedProject;

    function clearSuggestion() {
        suggestionStatus.textContent = "";
        suggestionStatus.removeAttribute("lang");
        edit.hidden = true;
        clearError(suggestionError);
    }

    async function suggestPositions() {
        if (pending) {
            return;
        }

        const project = { name: draft.name, description: draft.description };
        suggestedProject = project;
        clearSuggestion();
        pending = true;
        suggest.setAttribute("aria-disabled", "true");
        suggest.textContent = "Suggesting…";

        try {
            const result = await suggestTeam(project);
            if (
                !dialog.open ||
                section.hidden ||
                !isCurrentProject(project, form)
            ) {
                return;
            }

            if (result.question) {
                suggestionStatus.lang = result.language;
                suggestionStatus.textContent = result.question;
                edit.hidden = false;
                return;
            }

            if (
                hasDraftPositions(draft) &&
                !window.confirm(
                    "Replace the current positions and their skills?",
                )
            ) {
                return;
            }

            const restoreFocus = people.contains(document.activeElement);
            draft.positions = result.positions;

            renderPeople();

            if (restoreFocus) {
                people.querySelector('[data-field="title"]').focus();
            }
            suggestionStatus.textContent =
                "Positions and skills added. You can edit them before publishing.";
        } catch (cause) {
            if (
                dialog.open &&
                !section.hidden &&
                isCurrentProject(project, form)
            ) {
                showError(
                    suggestionError,
                    cause.message || errorMessages.requestFailed,
                );
            }
        } finally {
            pending = false;
            suggest.removeAttribute("aria-disabled");
            suggest.textContent = "Suggest Team (AI)";
        }
    }

    function updatePeopleCount() {
        const count = totalPlaces(draft);
        const includesCaptain = hasCaptain(draft);
        countLabel.textContent = `${formatPlaces(count)} · ${count - Number(includesCaptain)} people to recruit${includesCaptain ? " · You are included" : " · You are not assigned to a position"}`;
        if (count <= MAX_PEOPLE) {
            clearError(peopleError);
        }
        add.disabled = draft.positions.length >= MAX_PEOPLE;
    }

    function renderPeople() {
        people.replaceChildren();

        draft.positions.forEach((position, index) => {
            const row = template.content.firstElementChild.cloneNode(true);
            row.dataset.index = String(index);
            row.querySelector("legend").textContent = `Position ${index + 1}`;

            for (const field of row.querySelectorAll("[data-field]")) {
                const key = field.dataset.field;
                field.id = `project-position-${index}-${key}`;
                row.querySelector(`[data-label="${key}"]`).htmlFor = field.id;
                if (key === "categoryId") {
                    field.add(new Option("Choose a category", ""));
                    for (const category of catalog.categories) {
                        field.add(
                            new Option(category.name, String(category.id)),
                        );
                    }
                }
                field.value = position[key] || "";
                if (field.hasAttribute("data-counted")) {
                    const counter = field.parentElement.querySelector(
                        "[data-character-count]",
                    );
                    counter.id = `${field.id}-count`;
                    field.setAttribute("aria-describedby", counter.id);
                    updateCharacterCount(field);
                }
            }

            const captain = row.querySelector("[data-include-captain]");
            captain.textContent = position.includeCaptain
                ? "Remove yourself"
                : "Add yourself";
            captain.setAttribute(
                "aria-pressed",
                String(position.includeCaptain),
            );
            row.querySelector("[data-remove-position]").disabled =
                draft.positions.length === 1;
            people.append(row);
        });

        updatePeopleCount();
    }

    function addPosition() {
        if (draft.positions.length >= MAX_PEOPLE) {
            return;
        }
        draft.positions.push(createPosition());
        renderPeople();
    }

    people.addEventListener("input", (event) => {
        const field = event.target.closest("[data-field]");
        if (!field) {
            return;
        }
        const position =
            draft.positions[
                Number(field.closest("[data-index]").dataset.index)
            ];
        position[field.dataset.field] =
            field.dataset.field === "title"
                ? field.value.trim()
                : Number(field.value);
        updatePeopleCount();
    });
    people.addEventListener("click", (event) => {
        const button = event.target.closest("button");
        if (!button) {
            return;
        }
        const index = Number(button.closest("[data-index]").dataset.index);
        if (
            button.hasAttribute("data-remove-position") &&
            draft.positions.length > 1
        ) {
            draft.positions.splice(index, 1);
            renderPeople();
            people.querySelector('[data-field="title"]').focus();
        } else if (button.hasAttribute("data-include-captain")) {
            toggleCaptain(draft, index);
            renderPeople();
            people.children[index]
                .querySelector("[data-include-captain]")
                .focus();
        }
    });
    add.addEventListener("click", () => {
        addPosition();
        people.lastElementChild.querySelector("input").focus();
    });
    suggest.addEventListener("click", suggestPositions);
    edit.addEventListener("click", editDescription);

    return {
        section,
        enter() {
            if (!isCurrentProject(suggestedProject, form)) {
                clearSuggestion();
            }
            if (!draft.positions.length) {
                draft.positions.push(createPosition());
            }
            renderPeople();
        },
        validate() {
            for (const field of people.querySelectorAll(
                '[data-field="title"]',
            )) {
                field.setCustomValidity(
                    field.value.trim() ? "" : "Enter a position title.",
                );
            }

            if (!reportFirstInvalid(section)) {
                return false;
            }

            if (totalPlaces(draft) > MAX_PEOPLE) {
                showError(
                    peopleError,
                    `Use up to ${MAX_PEOPLE} places in total.`,
                );
                if (!section.hidden) {
                }
                return false;
            }

            clearError(peopleError);
            return true;
        },
        reset() {
            people.replaceChildren();
            clearError(peopleError);
            clearSuggestion();
            suggestedProject = undefined;
            updatePeopleCount();
        },
    };
}
