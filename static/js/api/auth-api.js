import { HttpError } from "@shared/http.js";
import { errorMessages } from "@shared/constants/error-messages.js";
import { api } from "./client.js";

export function signUp(data) {
    return api.post("/user/signup", data);
}

export function signIn(data) {
    return api.post("/user/signin", data);
}

export function signOut() {
    return api.post("/user/signout");
}

export async function getCurrentUser() {
    try {
        const { data: user } = await api.get("/user/me");
        if (typeof user?.fullName !== "string") {
            throw new Error(errorMessages.requestFailed);
        }
        return user;
    } catch (error) {
        if (error instanceof HttpError && error.status === 401) {
            return null;
        }
        throw error;
    }
}

export async function getProfile() {
    const { data } = await api.get("/user/profile");
    return data;
}

export function saveSocialLinks(links) {
    return api.post("/user/social-links", links);
}

export function saveSkills(skills) {
    return api.post("/user/skills", skills);
}
