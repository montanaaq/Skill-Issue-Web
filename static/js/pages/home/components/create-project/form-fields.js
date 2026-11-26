export function isCurrentProject(project, form) {
    if (!project) {
        return false;
    }

    const { name, description } = form.elements;
    return (
        project.name === name.value.trim() &&
        project.description === description.value.trim()
    );
}

export function updateCharacterCount(field) {
    field.parentElement.querySelector("[data-character-count]").textContent =
        `${Math.max(0, field.maxLength - field.value.length)} left`;
}

export function reportFirstInvalid(section) {
    const invalid = [
        ...section.querySelectorAll("input, textarea, select"),
    ].find((field) => !field.checkValidity());
    if (!invalid) {
        return true;
    }
    if (!section.hidden) {
        const disclosure = invalid.closest("details");
        if (disclosure) {
            disclosure.open = true;
        }
        invalid.reportValidity();
    }
    return false;
}
