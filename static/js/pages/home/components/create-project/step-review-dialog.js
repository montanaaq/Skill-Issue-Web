export function createReviewStep(section, draft, catalog) {
    const review = section.querySelector("#project-review");
    const template = document.getElementById(
        "project-review-position-template",
    );
    const skillNames = new Map(
        catalog.skills.map((skill) => [skill.id, skill.name]),
    );
    const categoryNames = new Map(
        catalog.categories.map((category) => [category.id, category.name]),
    );

    function enter() {
        const heading = document.createElement("h4");
        heading.textContent = draft.name;

        const description = document.createElement("p");
        description.className = "project-review-description";
        description.textContent = draft.description;

        const positions = document.createElement("ul");
        positions.className = "project-review-positions";
        positions.setAttribute("role", "list");

        for (const position of draft.positions) {
            const item = template.content.firstElementChild.cloneNode(true);
            const openPlaces = position.count - Number(position.includeCaptain);

            item.querySelector("[data-review-title]").textContent =
                position.title;
            item.querySelector("[data-review-category]").textContent =
                categoryNames.get(position.categoryId);
            item.querySelector("[data-review-places]").textContent =
                position.count;
            item.querySelector("[data-review-open]").textContent = openPlaces;
            item.querySelector("[data-review-captain]").hidden =
                !position.includeCaptain;

            const skills = item.querySelector("[data-review-skills]");
            for (const { skillId, requiredLevel, importance } of position.skills) {
                const skill = document.createElement("li");
                skill.textContent = `${skillNames.get(skillId)} · Level ${requiredLevel}/3 · Importance ${importance}/3`;
                skills.append(skill);
            }

            positions.append(item);
        }

        review.replaceChildren(heading, description, positions);
    }

    return {
        section,
        enter,
        validate: () => true,
        reset: () => review.replaceChildren(),
    };
}
