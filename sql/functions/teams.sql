CREATE INDEX IF NOT EXISTS team_positions_open_team_idx ON public.team_positions (team_id)
WHERE is_open
    AND deleted_at IS NULL;
CREATE OR REPLACE FUNCTION public.get_my_projects(viewer_id INTEGER) RETURNS JSONB LANGUAGE sql STABLE AS $$
SELECT COALESCE(
        jsonb_agg(
            jsonb_build_object(
                'id',
                t.id,
                'name',
                t.name,
                'description',
                t.description,
                'role',
                CASE
                    WHEN t.captain_id = viewer_id THEN 'Captain'
                    ELSE 'Member'
                END,
                'status',
                CASE
                    WHEN t.closed_at IS NULL THEN 'Active'
                    ELSE 'Closed'
                END,
                'memberCount',
                (
                    SELECT count(*)::INTEGER
                    FROM public.team_members m
                    WHERE m.team_id = t.id
                        AND m.left_at IS NULL
                ),
                'positions',
                ARRAY(
                    SELECT mp.position_title_at_assignment
                    FROM public.team_member_positions mp
                    WHERE mp.membership_id = membership.id
                        AND mp.ended_at IS NULL
                    ORDER BY mp.id
                ),
                'incomingApplications',
                CASE
                    WHEN t.captain_id = viewer_id THEN COALESCE(
                        (
                            SELECT jsonb_agg(
                                    jsonb_build_object(
                                        'id',
                                        a.id,
                                        'applicantName',
                                        u.full_name,
                                        'applicantEmail',
                                        u.email,
                                        'position',
                                        p.title,
                                        'coverLetter',
                                        a.cover_letter,
                                        'status',
                                        a.status,
                                        'resolutionReason',
                                        a.resolution_reason,
                                        'createdAt',
                                        a.created_at,
                                        'canAccept',
                                        t.closed_at IS NULL
                                        AND p.is_open
                                        AND p.deleted_at IS NULL,
                                        'match',
                                        jsonb_build_object(
                                            'skills',
                                            COALESCE(
                                                (
                                                    SELECT jsonb_agg(
                                                            jsonb_build_object(
                                                                'name',
                                                                s.name,
                                                                'requiredLevel',
                                                                r.required_level,
                                                                'importance',
                                                                r.importance,
                                                                'candidateLevel',
                                                                us.skill_level
                                                            )
                                                            ORDER BY r.importance DESC,
                                                                s.name,
                                                                s.id
                                                        )
                                                    FROM public.position_required_skills r
                                                        JOIN public.skills s ON s.id = r.skill_id
                                                        LEFT JOIN public.user_skills us ON us.user_id = a.applicant_id
                                                        AND us.skill_id = r.skill_id
                                                    WHERE r.position_id = p.id
                                                ),
                                                '[]'::JSONB
                                            )
                                        )
                                    )
                                    ORDER BY (a.status = 'pending') DESC,
                                        a.created_at DESC,
                                        a.id DESC
                                )
                            FROM public.team_positions p
                                JOIN public.applications a ON a.position_id = p.id
                                JOIN public.users u ON u.id = a.applicant_id
                            WHERE p.team_id = t.id
                        ),
                        '[]'::JSONB
                    )
                END
            )
            ORDER BY t.name,
                t.id
        ),
        '[]'::JSONB
    )
FROM public.team_members membership
    JOIN public.teams t ON t.id = membership.team_id
WHERE membership.user_id = viewer_id
    AND membership.left_at IS NULL
    AND t.deleted_at IS NULL;
$$;
BEGIN;
DROP FUNCTION IF EXISTS public.get_team_catalog();
CREATE FUNCTION public.get_team_catalog() RETURNS TABLE (
    kind TEXT,
    id INTEGER,
    name TEXT,
    category_id INTEGER,
    related_skill_ids INTEGER []
) LANGUAGE sql STABLE AS $$ WITH active_skills AS (
    SELECT s.id,
        s.name,
        s.category_id
    FROM public.skills AS s
        JOIN public.skill_categories AS c ON c.id = s.category_id
    WHERE s.is_active
        AND c.is_active
),
relations AS (
    SELECT first_skill_id AS skill_id,
        second_skill_id AS related_id
    FROM public.skill_relations
    UNION ALL
    SELECT second_skill_id,
        first_skill_id
    FROM public.skill_relations
),
related_skills AS (
    SELECT r.skill_id,
        array_agg(
            r.related_id
            ORDER BY r.related_id
        ) AS ids
    FROM relations AS r
        JOIN active_skills AS s ON s.id = r.related_id
    GROUP BY r.skill_id
)
SELECT 'skill',
    s.id,
    s.name::TEXT,
    s.category_id,
    COALESCE(r.ids, ARRAY []::INTEGER [])
FROM active_skills AS s
    LEFT JOIN related_skills AS r ON r.skill_id = s.id
UNION ALL
SELECT 'category',
    c.id,
    c.name::TEXT,
    NULL::INTEGER,
    NULL::INTEGER []
FROM public.position_categories c
WHERE c.is_active
ORDER BY 1,
    3;
$$;
COMMIT;
BEGIN;
DROP FUNCTION IF EXISTS public.search_teams(INTEGER [], INTEGER, INTEGER);
DROP FUNCTION IF EXISTS public.search_teams(INTEGER [], INTEGER);
CREATE OR REPLACE FUNCTION public.search_teams(
        selected_skills INTEGER [],
        before_id INTEGER,
        match_any_skill BOOLEAN DEFAULT FALSE
    ) RETURNS TABLE (
        id INTEGER,
        name TEXT,
        description TEXT,
        member_count INTEGER,
        positions TEXT [],
        required_skills TEXT [],
        position_ids INTEGER []
    ) LANGUAGE sql STABLE AS $$
SELECT t.id,
    t.name::TEXT,
    t.description::TEXT,
    (
        SELECT count(*)::INTEGER
        FROM public.team_members m
        WHERE m.team_id = t.id
            AND m.left_at IS NULL
    ),
    ARRAY(
        SELECT p.title::TEXT
        FROM public.team_positions p
        WHERE p.team_id = t.id
            AND p.is_open
            AND p.deleted_at IS NULL
        ORDER BY p.id
    ),
    ARRAY(
        SELECT DISTINCT s.name::TEXT
        FROM public.team_positions p
            JOIN public.position_required_skills r ON r.position_id = p.id
            JOIN public.skills s ON s.id = r.skill_id
            JOIN public.skill_categories c ON c.id = s.category_id
        WHERE p.team_id = t.id
            AND p.is_open
            AND p.deleted_at IS NULL
            AND c.is_active
        ORDER BY s.name::TEXT
    ),
    ARRAY(
        SELECT p.id
        FROM public.team_positions p
        WHERE p.team_id = t.id
            AND p.is_open
            AND p.deleted_at IS NULL
        ORDER BY p.id
    )
FROM public.teams t
WHERE t.closed_at IS NULL
    AND t.deleted_at IS NULL
    AND t.id < before_id
    AND EXISTS (
        SELECT 1
        FROM public.team_positions p
        WHERE p.team_id = t.id
            AND p.is_open
            AND p.deleted_at IS NULL
    )
    AND (
        COALESCE(cardinality(selected_skills), 0) = 0
        OR (
            SELECT count(*)
            FROM unnest(selected_skills) AS selected(skill_id)
            WHERE EXISTS (
                    SELECT 1
                    FROM public.team_positions p
                        JOIN public.position_required_skills r ON r.position_id = p.id
                        JOIN public.skills s ON s.id = r.skill_id
                        JOIN public.skill_categories c ON c.id = s.category_id
                    WHERE p.team_id = t.id
                        AND p.is_open
                        AND p.deleted_at IS NULL
                        AND s.is_active
                        AND c.is_active
                        AND r.skill_id = selected.skill_id
                )
        ) >= CASE
            WHEN match_any_skill THEN 1
            ELSE cardinality(selected_skills)
        END
    )
ORDER BY t.id DESC
LIMIT 13;
$$;
CREATE OR REPLACE FUNCTION public.search_teams(
        selected_skills INTEGER [],
        before_id INTEGER,
        viewer_id INTEGER,
        match_any_skill BOOLEAN DEFAULT FALSE
    ) RETURNS TABLE (
        id INTEGER,
        name TEXT,
        description TEXT,
        member_count INTEGER,
        positions TEXT [],
        required_skills TEXT [],
        position_ids INTEGER [],
        is_member BOOLEAN,
        application_positions JSONB,
        has_pending_application BOOLEAN
    ) LANGUAGE sql STABLE AS $$
SELECT found.id,
    found.name,
    found.description,
    found.member_count,
    found.positions,
    found.required_skills,
    found.position_ids,
    COALESCE(t.captain_id = viewer_id, FALSE)
    OR EXISTS (
        SELECT 1
        FROM public.team_members m
        WHERE m.team_id = found.id
            AND m.user_id = viewer_id
            AND m.left_at IS NULL
    ),
    (
        SELECT jsonb_agg(
                jsonb_build_object(
                    'id',
                    p.id,
                    'title',
                    p.title,
                    'category',
                    c.name,
                    'skills',
                    ARRAY(
                        SELECT s.name
                        FROM public.position_required_skills r
                            JOIN public.skills s ON s.id = r.skill_id
                            JOIN public.skill_categories sc ON sc.id = s.category_id
                        WHERE r.position_id = p.id
                            AND sc.is_active
                        ORDER BY s.name
                    ),
                    'hasPendingApplication',
                    EXISTS (
                        SELECT 1
                        FROM public.applications a
                        WHERE a.position_id = p.id
                            AND a.applicant_id = viewer_id
                            AND a.status = 'pending'
                    )
                )
                ORDER BY p.id
            )
        FROM public.team_positions p
            JOIN public.position_categories c ON c.id = p.category_id
        WHERE p.id = ANY(found.position_ids)
    ),
    EXISTS (
        SELECT 1
        FROM public.applications a
            JOIN public.team_positions p ON p.id = a.position_id
        WHERE p.team_id = t.id
            AND a.applicant_id = viewer_id
            AND a.status = 'pending'
    )
FROM public.search_teams(selected_skills, before_id, match_any_skill) found
    JOIN public.teams t ON t.id = found.id
ORDER BY found.id DESC;
$$;
COMMIT;
BEGIN;
DROP FUNCTION IF EXISTS public.get_project_recommendations(INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION public.get_project_recommendations(
        candidate_id INTEGER,
        requested_position_id INTEGER,
        result_limit INTEGER
    ) RETURNS JSONB LANGUAGE sql STABLE AS $$ WITH open_positions AS (
        SELECT p.id,
            p.team_id,
            p.title,
            c.name AS category
        FROM public.team_positions p
            JOIN public.teams t ON t.id = p.team_id
            JOIN public.position_categories c ON c.id = p.category_id
        WHERE (
                requested_position_id IS NULL
                OR p.id = requested_position_id
            )
            AND p.is_open
            AND p.deleted_at IS NULL
            AND t.closed_at IS NULL
            AND t.deleted_at IS NULL
            AND t.captain_id <> candidate_id
            AND NOT EXISTS (
                SELECT 1
                FROM public.team_members m
                WHERE m.team_id = t.id
                    AND m.user_id = candidate_id
                    AND m.left_at IS NULL
            )
            AND NOT EXISTS (
                SELECT 1
                FROM public.applications a
                    JOIN public.team_positions applied ON applied.id = a.position_id
                WHERE applied.team_id = t.id
                    AND a.applicant_id = candidate_id
                    AND a.status = 'pending'
            )
    ),
    position_matches AS (
        SELECT p.id,
            p.team_id,
            p.title,
            p.category,
            100 * sum(
                LEAST(COALESCE(us.skill_level, 0), r.required_level)::NUMERIC / r.required_level * r.importance
            ) / sum(r.importance) AS percent,
            jsonb_agg(
                jsonb_build_object(
                    'name',
                    s.name,
                    'requiredLevel',
                    r.required_level,
                    'importance',
                    r.importance,
                    'candidateLevel',
                    us.skill_level
                )
                ORDER BY s.name,
                    s.id
            ) AS skills
        FROM open_positions p
            JOIN public.position_required_skills r ON r.position_id = p.id
            JOIN public.skills s ON s.id = r.skill_id
            AND s.is_active
            JOIN public.skill_categories c ON c.id = s.category_id
            AND c.is_active
            LEFT JOIN public.user_skills us ON us.skill_id = s.id
            AND us.user_id = candidate_id
        GROUP BY p.id,
            p.team_id,
            p.title,
            p.category
        HAVING count(us.skill_id) > 0
    ),
    best_positions AS (
        SELECT DISTINCT ON (team_id) id,
            team_id,
            title,
            category,
            percent,
            skills
        FROM position_matches
        ORDER BY team_id,
            percent DESC,
            id
    ),
    top_matches AS (
        SELECT id,
            team_id,
            title,
            category,
            percent,
            skills
        FROM best_positions
        ORDER BY percent DESC,
            team_id DESC
        LIMIT result_limit
    )
SELECT COALESCE(
        jsonb_agg(
            jsonb_build_object(
                'teamId',
                t.id,
                'teamName',
                t.name,
                'description',
                t.description,
                'positionId',
                m.id,
                'positionTitle',
                m.title,
                'positionCategory',
                m.category,
                'percent',
                round(m.percent, 2),
                'skills',
                m.skills
            )
            ORDER BY m.percent DESC,
                t.id DESC
        ),
        '[]'::JSONB
    )
FROM top_matches m
    JOIN public.teams t ON t.id = m.team_id;
$$;
COMMIT;
CREATE OR REPLACE FUNCTION public.create_team(
        captain INTEGER,
        team_name TEXT,
        team_description TEXT,
        positions JSONB
    ) RETURNS INTEGER LANGUAGE plpgsql AS $$
DECLARE new_team_id INTEGER;
new_position_id INTEGER;
membership INTEGER;
position RECORD;
slot INTEGER;
BEGIN IF jsonb_typeof(positions) IS DISTINCT
FROM 'array' THEN RETURN NULL;
END IF;
IF jsonb_array_length(positions) NOT BETWEEN 1 AND 20 THEN RETURN NULL;
END IF;
IF EXISTS (
    SELECT 1
    FROM jsonb_to_recordset(positions) AS p(
            title TEXT,
            "categoryId" INTEGER,
            count INTEGER,
            "includeCaptain" BOOLEAN,
            skills JSONB
        )
    WHERE p.title IS NULL
        OR length(trim(p.title)) NOT BETWEEN 1 AND 120
        OR p.count IS NULL
        OR p.count NOT BETWEEN 1 AND 20
        OR p."includeCaptain" IS NULL
        OR NOT EXISTS (
            SELECT 1
            FROM public.position_categories c
            WHERE c.id = p."categoryId"
                AND c.is_active
        )
        OR NOT (
            SELECT count(*) BETWEEN 1 AND 20
                AND count(*) = count(DISTINCT s.id) FILTER (
                    WHERE c.id IS NOT NULL
                        AND required."requiredLevel" BETWEEN 1 AND 3
                        AND required.importance BETWEEN 1 AND 3
                )
            FROM jsonb_to_recordset(
                    CASE
                        WHEN jsonb_typeof(p.skills) = 'array' THEN p.skills
                        ELSE '[]'::JSONB
                    END
                ) AS required(
                    "skillId" INTEGER,
                    "requiredLevel" SMALLINT,
                    importance SMALLINT
                )
                LEFT JOIN public.skills s ON s.id = required."skillId"
                AND s.is_active
                LEFT JOIN public.skill_categories c ON c.id = s.category_id
                AND c.is_active
        )
)
OR (
    SELECT sum(p.count) > 20
        OR count(*) FILTER (
            WHERE p."includeCaptain"
        ) > 1
    FROM jsonb_to_recordset(positions) AS p(count INTEGER, "includeCaptain" BOOLEAN)
) THEN RETURN NULL;
END IF;
INSERT INTO public.teams (captain_id, name, description)
VALUES (captain, trim(team_name), trim(team_description))
RETURNING id INTO new_team_id;
INSERT INTO public.team_members (team_id, user_id)
VALUES (new_team_id, captain)
RETURNING id INTO membership;
FOR position IN
SELECT *
FROM jsonb_to_recordset(positions) AS p(
        title TEXT,
        "categoryId" INTEGER,
        count INTEGER,
        "includeCaptain" BOOLEAN,
        skills JSONB
    ) LOOP FOR slot IN 1..position.count LOOP
INSERT INTO public.team_positions (team_id, category_id, title, is_open)
VALUES (
        new_team_id,
        position."categoryId",
        trim(position.title),
        NOT (
            position."includeCaptain"
            AND slot = 1
        )
    )
RETURNING id INTO new_position_id;
INSERT INTO public.position_required_skills (
        position_id,
        skill_id,
        required_level,
        importance
    )
SELECT new_position_id,
    required."skillId",
    required."requiredLevel",
    required.importance
FROM jsonb_to_recordset(position.skills) AS required(
        "skillId" INTEGER,
        "requiredLevel" SMALLINT,
        importance SMALLINT
    );
IF position."includeCaptain"
AND slot = 1 THEN
INSERT INTO public.team_member_positions (
        membership_id,
        position_id,
        position_title_at_assignment
    )
VALUES (
        membership,
        new_position_id,
        trim(position.title)
    );
END IF;
END LOOP;
END LOOP;
INSERT INTO public.team_events (team_id, event_type, actor_user_id)
VALUES (new_team_id, 'team_created', captain);
RETURN new_team_id;
END;
$$;
CREATE OR REPLACE FUNCTION public.create_team(
        captain INTEGER,
        team_name TEXT,
        team_description TEXT,
        position_title TEXT,
        position_category INTEGER,
        skill_ids INTEGER []
    ) RETURNS INTEGER LANGUAGE sql AS $$
SELECT public.create_team(
        captain,
        team_name,
        team_description,
        jsonb_build_array(
            jsonb_build_object(
                'title',
                position_title,
                'categoryId',
                position_category,
                'count',
                1,
                'includeCaptain',
                false,
                'skills',
                (
                    SELECT jsonb_agg(
                            jsonb_build_object(
                                'skillId',
                                skill_id,
                                'requiredLevel',
                                1,
                                'importance',
                                2
                            )
                        )
                    FROM unnest(skill_ids) AS selected(skill_id)
                )
            )
        )
    );
$$;
