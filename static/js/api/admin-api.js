import { api } from "./client.js";

export async function getAdminCatalog() {
    const { data } = await api.get("/admin/catalog");
    return data;
}

export function setCatalogItemActive(kind, id, isActive) {
    return api.post(`/admin/catalog/${kind}/${id}`, { isActive });
}
