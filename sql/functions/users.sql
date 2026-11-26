CREATE OR REPLACE FUNCTION public.create_user(
        user_email TEXT,
        user_full_name TEXT,
        user_password_hash TEXT
    ) RETURNS INTEGER LANGUAGE sql AS $$
INSERT INTO public.users (email, full_name, password_hash)
VALUES (user_email, user_full_name, user_password_hash) ON CONFLICT (lower(email)) DO NOTHING
RETURNING id;
$$;
CREATE OR REPLACE FUNCTION public.find_user_by_email(user_email TEXT) RETURNS SETOF public.users LANGUAGE sql STABLE AS $$
SELECT u.*
FROM public.users AS u
WHERE lower(u.email) = lower(user_email);
$$;
CREATE OR REPLACE FUNCTION public.find_user_by_id(user_id INTEGER) RETURNS SETOF public.users LANGUAGE sql STABLE AS $$
SELECT u.*
FROM public.users AS u
WHERE u.id = user_id;
$$;
CREATE INDEX IF NOT EXISTS team_members_current_user_idx ON public.team_members (user_id, team_id)
WHERE left_at IS NULL;
CREATE INDEX IF NOT EXISTS applications_applicant_idx ON public.applications (applicant_id, created_at DESC);
CREATE INDEX IF NOT EXISTS applications_position_idx ON public.applications (position_id);
CREATE OR REPLACE FUNCTION public.get_user_profile(profile_user_id INTEGER) RETURNS JSONB LANGUAGE plpgsql STABLE AS $$
DECLARE social_links JSONB;
user_skills TEXT [];
active_skill_ids INTEGER [];
editable_skills JSONB;
user_applications JSONB;
BEGIN
SELECT jsonb_agg(
        jsonb_build_object('network', network, 'url', url)
        ORDER BY id
    ) INTO social_links
FROM public.user_social_links
WHERE user_id = profile_user_id;
SELECT array_agg(
        s.name
        ORDER BY s.name
    ),
    array_agg(
        s.id
        ORDER BY s.name
    ) FILTER (
        WHERE s.is_active AND c.is_active
    ),
    jsonb_agg(
        jsonb_build_object('skillId', s.id, 'skillLevel', us.skill_level)
        ORDER BY s.name
    ) FILTER (
        WHERE s.is_active AND c.is_active
    ) INTO user_skills,
    active_skill_ids,
    editable_skills
FROM public.user_skills us
    JOIN public.skills s ON s.id = us.skill_id
    JOIN public.skill_categories c ON c.id = s.category_id
WHERE us.user_id = profile_user_id;
WITH sent_applications AS (
    SELECT a.id,
        t.name AS "teamName",
        t.description,
        p.title AS position,
        a.status,
        a.created_at AS "createdAt",
        a.cover_letter AS "coverLetter",
        a.resolution_reason AS "resolutionReason"
    FROM public.applications a
        JOIN public.team_positions p ON p.id = a.position_id
        JOIN public.teams t ON t.id = p.team_id
    WHERE a.applicant_id = profile_user_id
)
SELECT jsonb_agg(
        to_jsonb(sent_applications)
        ORDER BY "createdAt" DESC,
            id DESC
    ) INTO user_applications
FROM sent_applications;
RETURN jsonb_build_object(
    'links',
    COALESCE(social_links, '[]'::JSONB),
    'skills',
    COALESCE(user_skills, ARRAY []::TEXT []),
    'skillIds',
    COALESCE(active_skill_ids, ARRAY []::INTEGER []),
    'editableSkills',
    COALESCE(editable_skills, '[]'::JSONB),
    'applications',
    COALESCE(user_applications, '[]'::JSONB)
);
END;
$$;
