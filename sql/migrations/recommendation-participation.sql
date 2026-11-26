ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS show_in_recommendations BOOLEAN NOT NULL DEFAULT FALSE,
    DROP COLUMN IF EXISTS profile_private;
