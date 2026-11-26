CREATE OR REPLACE FUNCTION public.get_admin_catalog() RETURNS JSONB LANGUAGE sql STABLE AS $$
SELECT COALESCE(
        jsonb_agg(
            to_jsonb(item)
            ORDER BY item.name,
                item.id
        ),
        '[]'::JSONB
    )
FROM (
        SELECT 'skills' AS kind,
            id,
            name,
            is_active AS "isActive"
        FROM public.skills
        UNION ALL
        SELECT 'position-categories',
            id,
            name,
            is_active
        FROM public.position_categories
    ) AS item;
$$;