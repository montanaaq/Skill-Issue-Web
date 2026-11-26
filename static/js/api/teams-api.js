import { api } from "./client.js";

export async function getTeamCatalog() {
    const { data } = await api.get("/teams/catalog");
    return data;
}

export async function getMyProjects() {
    const { data } = await api.get("/teams/mine");
    return data;
}

export async function searchProjects(skillIds, before) {
    const { data } = await api.post("/teams/search", { skillIds, before });
    return data;
}

export async function createTeam(request) {
    const { data } = await api.post("/teams", request);
    return data;
}

export async function suggestTeam(project) {
    const { data } = await api.post("/teams/suggest", project);
    return data;
}

export async function getProjectRecommendations() {
    const { data } = await api.get("/teams/recommendations");
    return data;
}

export async function explainProjectMatch(positionId) {
    const { data } = await api.post(
        `/teams/recommendations/${positionId}/explanation`,
    );
    return data.explanation;
}

export async function applyToTeam(teamId, request) {
    const { data } = await api.post(`/teams/${teamId}/applications`, request);
    return data;
}

export async function resolveTeamApplication(teamId, applicationId, decision) {
    const { data } = await api.post(`/teams/${teamId}/applications/${applicationId}`, {
        decision,
    });
    return data;
}
