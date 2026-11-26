import { createSkillPicker } from "@shared/skill-picker.js";
import { formatPlaces } from "./draft.js";
import { reportFirstInvalid } from "./form-fields.js";

export function createSkillsStep(section, draft, catalog) {
    const skillGroups = section.querySelector("#project-position-skills");
    const template = document.getElementById("project-skill-template");
    let pickers = [];

    function reset() {
        for (const picker of pickers) {
            picker.destroy();
        }

        pickers = [];
        skillGroups.replaceChildren();
    }

    function enter() {
        reset();
        draft.positions.forEach((position, index) => {
            const group = template.content.firstElementChild.cloneNode(true);
            group.querySelector("legend").textContent =
                `${position.title} · ${formatPlaces(position.count)}`;

            if (position.includeCaptain) {
                group.querySelector("legend").textContent += " · including you";
            }

            const input = group.querySelector("input");
            input.id = `project-skills-${index}`;
            group.querySelector("label").htmlFor = input.id;

            const search = group.querySelector("details");
            search.before(search.querySelector(".selected-skills"));

            skillGroups.append(group);

            pickers.push(
                createSkillPicker(
                    group,
                    catalog.skills,
                    (ids) => {
                        position.skills = ids.map(
                            (skillId) =>
                                position.skills.find((skill) => skill.skillId === skillId)
                                ?? { skillId, requiredLevel: 1, importance: 2 },
                        );
                        input.setCustomValidity("");
                    },
                    position.skills.map((skill) => skill.skillId),
                    ({ id, name }, createSkillSelect) => {
                        const skill = position.skills.find((skill) => skill.skillId === id);
                        return [
                            createSkillSelect(
                                `${input.id}-${id}-level`,
                                "Required",
                                skill.requiredLevel,
                                (value) => { skill.requiredLevel = value; },
                                `Required level for ${name}`,
                            ),
                            createSkillSelect(
                                `${input.id}-${id}-importance`,
                                "Importance",
                                skill.importance,
                                (value) => { skill.importance = value; },
                                `Importance for ${name}`,
                            ),
                        ];
                    },
                ),
            );
        });
    }

    return {
        section,
        enter,
        validate() {
            const inputs = [...skillGroups.querySelectorAll('input[type="search"]')];

            for (const [index, input] of inputs.entries()) {
                input.setCustomValidity(
                    draft.positions[index].skills.length
                        ? ""
                        : "Choose at least one required skill.",
                );
            }

            return reportFirstInvalid(section);
        },
        reset,
    };
}
