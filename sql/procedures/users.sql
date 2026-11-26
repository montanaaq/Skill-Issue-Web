BEGIN;
DROP ROUTINE IF EXISTS public.save_user_social_links(INTEGER, JSONB);
CREATE PROCEDURE public.save_user_social_links(IN profile_user_id INTEGER, IN links JSONB) LANGUAGE plpgsql AS $$ BEGIN PERFORM 1
FROM public.users
WHERE id = profile_user_id FOR
UPDATE;
DELETE FROM public.user_social_links
WHERE user_id = profile_user_id;
INSERT INTO public.user_social_links (user_id, network, url)
SELECT profile_user_id,
    link.network,
    link.url
FROM jsonb_to_recordset(links) AS link(network TEXT, url TEXT);
END;
$$;
DROP ROUTINE IF EXISTS public.save_user_skills(INTEGER, INTEGER []);
CREATE OR REPLACE PROCEDURE public.save_user_skills(
    IN profile_user_id INTEGER,
    IN requested_skills JSONB,
    OUT saved BOOLEAN
) LANGUAGE plpgsql AS $$
BEGIN
    IF (
        SELECT count(DISTINCT s.id)
        FROM jsonb_to_recordset(requested_skills) AS requested("skillId" INTEGER, "skillLevel" SMALLINT)
        JOIN public.skills AS s ON s.id = requested."skillId" AND s.is_active
        JOIN public.skill_categories AS c ON c.id = s.category_id AND c.is_active
        WHERE requested."skillLevel" BETWEEN 1 AND 3
    ) <> jsonb_array_length(requested_skills) THEN
        saved := FALSE;
        RETURN;
    END IF;

    PERFORM 1
    FROM public.users
    WHERE id = profile_user_id
    FOR UPDATE;

    DELETE FROM public.user_skills AS us
    USING public.skills AS s
    JOIN public.skill_categories AS c ON c.id = s.category_id
    WHERE us.user_id = profile_user_id
        AND us.skill_id = s.id
        AND s.is_active
        AND c.is_active
        AND NOT EXISTS (
            SELECT 1
            FROM jsonb_to_recordset(requested_skills) AS requested("skillId" INTEGER)
            WHERE requested."skillId" = s.id
        );

    INSERT INTO public.user_skills (user_id, skill_id, skill_level)
    SELECT profile_user_id, requested."skillId", requested."skillLevel"
    FROM jsonb_to_recordset(requested_skills) AS requested("skillId" INTEGER, "skillLevel" SMALLINT)
    ON CONFLICT (user_id, skill_id) DO UPDATE
    SET skill_level = EXCLUDED.skill_level;

    saved := TRUE;
END;
$$;

COMMIT;
