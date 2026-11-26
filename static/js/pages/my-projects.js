import { getMyProjects, resolveTeamApplication } from "../api/teams-api.js";
import { $, textElement } from "@shared/dom.js";
import { APPLICATION_STATUSES } from "@shared/constants/applications.js";
import { clearError, showError } from "@shared/ui.js";
import { formatDate, renderProjects } from "./profile/render.js";

const projects = $("profile-teams");
const loading = $("loading");
const error = $("project-error");
const status = $("project-status");
const retry = $("retry");

function renderSkillMatch({ percent, skills }) {
    if (percent === null) {
        return textElement("p", "No skill requirements", "profile-meta");
    }

    const details = document.createElement("details");
    const summary = textElement(
        "summary",
        `${Math.round(percent)}% skill match`,
    );
    const list = document.createElement("ul");
    for (const { name, candidateLevel, requiredLevel, importance } of skills) {
        const level =
            candidateLevel === null ? "not listed" : `${candidateLevel}/3`;
        list.append(
            textElement(
                "li",
                `${name} · Candidate: ${level} · Required: ${requiredLevel}/3 · Importance: ${importance}/3`,
            ),
        );
    }
    details.append(summary, list);
    return details;
}

function renderIncoming(team) {
    const section = document.createElement("section");
    section.className = "project-incoming";
    section.append(textElement("h2", "Incoming applications"));
    if (team.incomingApplications.length === 0) {
        section.append(textElement("p", "No incoming applications yet."));
        return section;
    }

    const list = document.createElement("ul");
    list.setAttribute("role", "list");
    for (const application of team.incomingApplications) {
        const item = document.createElement("li");
        const heading = textElement("h3", application.applicantName);
        heading.id = `applicant-${application.id}`;
        item.append(
            heading,
            textElement("p", application.applicantEmail, "applicant-email"),
            textElement(
                "p",
                `${application.position} · ${formatDate(application.createdAt)}`,
                "profile-meta",
            ),
            textElement(
                "p",
                APPLICATION_STATUSES[application.status] ?? application.status,
            ),
        );
        if (application.resolutionReason) {
            item.append(
                textElement("p", application.resolutionReason, "project-body"),
            );
        }
        if (application.coverLetter) {
            item.append(
                textElement("p", application.coverLetter, "project-body"),
            );
        }
        item.append(renderSkillMatch(application.match));
        if (application.status === "pending") {
            const actions = document.createElement("div");
            actions.className = "project-application-actions";
            if (application.canAccept) {
                actions.append(
                    createDecisionButton(
                        team,
                        application,
                        heading.id,
                        "accept",
                        "Accept",
                    ),
                );
            }
            actions.append(
                createDecisionButton(
                    team,
                    application,
                    heading.id,
                    "reject",
                    "Reject",
                ),
            );
            item.append(actions);
        }
        list.append(item);
    }
    section.append(list);
    return section;
}

function createDecisionButton(team, application, headingId, decision, label) {
    const button = textElement(
        "button",
        label,
        decision === "accept" ? "btn btn-primary" : "btn btn-link",
    );
    button.type = "button";
    button.setAttribute("aria-describedby", headingId);
    button.addEventListener("click", () =>
        decide(team, application, decision, button),
    );
    return button;
}

async function loadProjects() {
    clearError(error);
    retry.hidden = true;
    loading.hidden = false;
    const openProjectIds = new Set(
        Array.from(
            projects.querySelectorAll(".project-row[open]"),
            (row) => row.id,
        ),
    );
    try {
        const teams = await getMyProjects();
        renderProjects(teams, renderIncoming, openProjectIds);
    } catch (cause) {
        if (cause.status === 401) {
            window.location.replace("/signin");
            return;
        }
        showError(error, cause.message);
        retry.hidden = false;
    } finally {
        loading.hidden = true;
    }
}

async function decide(team, application, decision, trigger) {
    clearError(error);
    status.textContent = "";
    const buttons = projects.querySelectorAll("button");
    for (const button of buttons) {
        button.disabled = true;
    }

    try {
        await resolveTeamApplication(team.id, application.id, decision);
        status.textContent =
            decision === "accept"
                ? "Application accepted."
                : "Application rejected.";
        await loadProjects();
    } catch (cause) {
        if (cause.status === 401) {
            window.location.replace("/signin");
            return;
        }
        if (cause.status === 409 || cause.status === 404) {
            await loadProjects();
        }
        showError(error, cause.message);
    } finally {
        for (const button of buttons) {
            button.disabled = false;
        }
        const focus = trigger.isConnected
            ? trigger
            : (document.querySelector(`#project-${team.id} > summary`) ??
              $("content"));
        focus.focus();
    }
}

retry.addEventListener("click", loadProjects);
loadProjects();
