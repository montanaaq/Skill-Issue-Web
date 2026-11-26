import { plural } from "@shared/dom.js";

export const createDraft = () => ({ name: "", description: "", positions: [] });
export const createPosition = () => ({
    title: "",
    categoryId: 0,
    count: 1,
    includeCaptain: false,
    skills: [],
});
export const totalPlaces = (draft) =>
    draft.positions.reduce((sum, position) => sum + position.count, 0);
export const hasCaptain = (draft) =>
    draft.positions.some((position) => position.includeCaptain);
export const formatPlaces = (count) => plural(count, "place", "places");

export function hasDraftPositions(draft) {
    if (draft.positions.length !== 1) {
        return draft.positions.length > 1;
    }

    const { title, categoryId, count, includeCaptain, skills } = draft.positions[0];
    const hasRole = Boolean(title || categoryId);
    const hasStaffingEdits = count !== 1 || includeCaptain;
    return hasRole || hasStaffingEdits || skills.length > 0;
}

export function toggleCaptain(draft, index) {
    const include = !draft.positions[index].includeCaptain;
    draft.positions.forEach((position, positionIndex) => {
        position.includeCaptain = positionIndex === index && include;
    });
}
