CREATE OR REPLACE PROCEDURE public.set_catalog_item_active(
        IN item_kind TEXT,
        IN item_id INTEGER,
        IN active BOOLEAN,
        OUT saved BOOLEAN
    ) LANGUAGE plpgsql AS $$ BEGIN IF item_kind = 'skills' THEN
UPDATE public.skills
SET is_active = active
WHERE id = item_id;
ELSIF item_kind = 'position-categories' THEN
UPDATE public.position_categories
SET is_active = active
WHERE id = item_id;
ELSE saved := FALSE;
RETURN;
END IF;
saved := FOUND;
END;
$$;