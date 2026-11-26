import { getCurrentUser } from "../../../../api/auth-api.js";
import { createTeam, getTeamCatalog } from "../../../../api/teams-api.js";
import { errorMessages } from "@shared/constants/error-messages.js";
import { clearError, showError } from "@shared/ui.js";
import { createStepper } from "../stepper.js";
import { createDraft } from "./draft.js";
import { updateCharacterCount } from "./form-fields.js";
import { createDetailsStep } from "./step-details-dialog.js";
import { createPositionsStep } from "./step-positions-dialog.js";
import { createSkillsStep } from "./step-skills-dialog.js";
import { createReviewStep } from "./step-review-dialog.js";

export function initCreateProject(ensureCatalog = getTeamCatalog) {
    const dialog = document.getElementById("create-team-dialog");
    const form = document.getElementById("create-team-form");
    const fields = document.getElementById("create-team-fields");
    const error = document.getElementById("create-team-error");
    const accountError = document.getElementById("account-error");
    const trigger = document.querySelector("[data-create-team]");
    const close = document.getElementById("close-create-team");
    const back = document.getElementById("project-back");
    const next = document.getElementById("project-next");
    const sections = [...form.querySelectorAll("[data-project-step]")];
    const markers = [...document.getElementById("project-steps").children];
    const draft = createDraft();
    const stepModules = [];
    const steps = createStepper(sections.length, showStep);
    let creating = false;

    function showStep(currentStep) {
        sections.forEach((section, index) => {
            section.hidden = index !== currentStep - 1;
            if (index === currentStep - 1) {
                markers[index].setAttribute("aria-current", "step");
            } else {
                markers[index].removeAttribute("aria-current");
            }
        });

        back.hidden = steps.isFirst;
        next.textContent = steps.isLast ? "Publish project" : "Continue";
        clearError(error);

        const current = stepModules[currentStep - 1];
        current.enter();

        if (dialog.open) {
            current.section.querySelector("h3").focus();
        }
    }

    function setBusy(isBusy) {
        creating = isBusy;
        fields.disabled = isBusy;
        close.disabled = isBusy;
        next.textContent = steps.isLast ? "Publish project" : "Continue";

        if (isBusy) {
            next.textContent = "Publishing…";
        }
    }

    async function openDialog() {
        clearError(accountError);
        try {
            const user = await getCurrentUser();
            if (user === null) {
                window.location.assign("/signin");
                return;
            }

            if (!stepModules.length) {
                const catalog = await ensureCatalog();
                if (stepModules.length) {
                    return;
                }
                stepModules.push(
                    createDetailsStep(sections[0], draft),
                    createPositionsStep(sections[1], draft, catalog, () => {
                        steps.set(1);
                        form.elements.description.focus();
                    }),
                    createSkillsStep(sections[2], draft, catalog),
                    createReviewStep(sections[3], draft, catalog),
                );
            }

            if (!dialog.open) {
                showStep(steps.currentStep);
                dialog.showModal();
            }
        } catch (cause) {
            showError(
                accountError,
                cause.message || errorMessages.requestFailed,
            );
        }
    }

    form.addEventListener("input", (event) => {
        if (event.target.hasAttribute("data-counted")) {
            updateCharacterCount(event.target);
        }
        if (event.target.setCustomValidity) {
            event.target.setCustomValidity("");
        }
    });

    trigger.addEventListener("click", openDialog);
    trigger.hidden = false;
    back.addEventListener("click", () => steps.back());

    close.addEventListener("click", () => {
        if (!creating) {
            dialog.close();
        }
    });

    dialog.addEventListener("cancel", (event) => {
        if (creating) {
            event.preventDefault();
        }
    });

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        if (creating || !stepModules[steps.currentStep - 1].validate()) {
            return;
        }

        if (!steps.isLast) {
            steps.next();
            return;
        }

        for (const [index, step] of stepModules.entries()) {
            if (!step.validate()) {
                steps.set(index + 1);
                step.validate();
                return;
            }
        }

        setBusy(true);
        clearError(error);

        try {
            await createTeam(draft);

            dialog.close();
            form.reset();
            Object.assign(draft, createDraft());
            stepModules.forEach((step) => step.reset());
            steps.reset();

            window.location.assign("/projects");
        } catch (cause) {
            showError(error, cause.message || errorMessages.requestFailed);
        } finally {
            setBusy(false);
        }
    });
}
