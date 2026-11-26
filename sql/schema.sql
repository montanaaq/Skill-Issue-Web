CREATE TYPE closure_kind AS ENUM ('owner', 'moderation');
CREATE TYPE application_status AS ENUM (
    'pending',
    'accepted',
    'rejected',
    'withdrawn',
    'cancelled'
);
CREATE TYPE invitation_status AS ENUM (
    'pending',
    'accepted',
    'declined',
    'cancelled'
);
-- 1. Users
CREATE TABLE users (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email VARCHAR(254) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(16) NOT NULL DEFAULT 'user',
    show_in_recommendations BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (role IN ('user', 'admin')),
    CHECK (length(trim(full_name)) > 0),
    CHECK (length(trim(email)) > 0),
    CHECK (updated_at >= created_at)
);
CREATE UNIQUE INDEX users_email_unique ON users (lower(email));
-- 2. Social links
CREATE TABLE user_social_links (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    network VARCHAR(80) NOT NULL,
    url VARCHAR(500) NOT NULL,
    UNIQUE (user_id, network),
    CHECK (length(trim(network)) > 0),
    CHECK (length(trim(url)) > 0)
);
-- 3. Skill categories
CREATE TABLE skill_categories (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(80) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CHECK (length(trim(name)) > 0)
);
CREATE UNIQUE INDEX skill_categories_name_unique ON skill_categories (lower(name));
-- 4. Skills
CREATE TABLE skills (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    category_id INTEGER NOT NULL REFERENCES skill_categories(id) ON DELETE RESTRICT,
    name VARCHAR(80) NOT NULL,
    description VARCHAR(1000),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CHECK (length(trim(name)) > 0)
);
CREATE UNIQUE INDEX skills_name_unique ON skills (lower(name));
CREATE TABLE skill_relations (
    first_skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE RESTRICT,
    second_skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE RESTRICT,
    PRIMARY KEY (first_skill_id, second_skill_id),
    CHECK (first_skill_id < second_skill_id)
);
CREATE INDEX skill_relations_second_skill_idx ON skill_relations (second_skill_id);
-- 5. Current user skills
CREATE TABLE user_skills (
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE RESTRICT,
    skill_level SMALLINT NOT NULL,
    PRIMARY KEY (user_id, skill_id),
    CHECK (
        skill_level BETWEEN 1 AND 3
    )
);
-- 6. Teams
CREATE TABLE teams (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    captain_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    name VARCHAR(120) NOT NULL,
    description VARCHAR(5000) NOT NULL,
    closed_at TIMESTAMPTZ,
    closure_kind closure_kind,
    deleted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (length(trim(name)) > 0),
    CHECK (length(trim(description)) > 0),
    CHECK ((closed_at IS NULL) = (closure_kind IS NULL)),
    CHECK (closed_at >= created_at),
    CHECK (deleted_at >= closed_at),
    CHECK (
        deleted_at IS NULL
        OR closed_at IS NOT NULL
    ),
    CHECK (updated_at >= created_at)
);
-- 7. Position categories
CREATE TABLE position_categories (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(80) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CHECK (length(trim(name)) > 0)
);
CREATE UNIQUE INDEX position_categories_name_unique ON position_categories (lower(name));
-- 8. Positions within teams
CREATE TABLE team_positions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    team_id INTEGER NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    category_id INTEGER NOT NULL REFERENCES position_categories(id) ON DELETE RESTRICT,
    title VARCHAR(120) NOT NULL,
    description VARCHAR(2000),
    is_open BOOLEAN NOT NULL DEFAULT TRUE,
    deleted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (length(trim(title)) > 0),
    CHECK (updated_at >= created_at),
    CHECK (deleted_at >= created_at),
    CHECK (
        deleted_at IS NULL
        OR NOT is_open
    )
);
-- 9. Current requirements of a position
CREATE TABLE position_required_skills (
    position_id INTEGER NOT NULL REFERENCES team_positions(id) ON DELETE RESTRICT,
    skill_id INTEGER NOT NULL REFERENCES skills(id) ON DELETE RESTRICT,
    required_level SMALLINT NOT NULL,
    importance SMALLINT NOT NULL DEFAULT 2,
    PRIMARY KEY (position_id, skill_id),
    CHECK (
        required_level BETWEEN 1 AND 3
    ),
    CHECK (
        importance BETWEEN 1 AND 3
    )
);
-- 10. Membership periods
CREATE TABLE team_members (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    team_id INTEGER NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    left_at TIMESTAMPTZ,
    CHECK (left_at >= joined_at)
);
CREATE UNIQUE INDEX team_members_active_unique ON team_members (team_id, user_id)
WHERE left_at IS NULL;
-- 11. Position assignment periods
CREATE TABLE team_member_positions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    membership_id INTEGER NOT NULL REFERENCES team_members(id) ON DELETE RESTRICT,
    position_id INTEGER NOT NULL REFERENCES team_positions(id) ON DELETE RESTRICT,
    -- Historical value captured when assigning the position.
    position_title_at_assignment VARCHAR(120) NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMPTZ,
    CHECK (length(trim(position_title_at_assignment)) > 0),
    CHECK (ended_at >= assigned_at)
);
CREATE UNIQUE INDEX team_member_positions_active_unique ON team_member_positions (membership_id, position_id)
WHERE ended_at IS NULL;
-- 12. Applications to positions
CREATE TABLE applications (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    position_id INTEGER NOT NULL REFERENCES team_positions(id) ON DELETE RESTRICT,
    applicant_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    cover_letter VARCHAR(2000),
    status application_status NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ,
    resolution_reason TEXT,
    CHECK ((status = 'pending') = (resolved_at IS NULL)),
    CHECK (resolved_at >= created_at)
);
CREATE UNIQUE INDEX applications_pending_unique ON applications (position_id, applicant_id)
WHERE status = 'pending';
-- 13. Invitations to positions
CREATE TABLE invitations (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    position_id INTEGER NOT NULL REFERENCES team_positions(id) ON DELETE RESTRICT,
    invitee_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    message VARCHAR(2000),
    status invitation_status NOT NULL DEFAULT 'pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ,
    CHECK ((status = 'pending') = (resolved_at IS NULL)),
    CHECK (resolved_at >= created_at)
);
CREATE UNIQUE INDEX invitations_pending_unique ON invitations (position_id, invitee_id)
WHERE status = 'pending';
-- 14. Authentication sessions
CREATE TABLE sessions (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    token_hash BYTEA NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    CHECK (octet_length(token_hash) = 32),
    CHECK (expires_at > created_at),
    CHECK (revoked_at >= created_at)
);
-- 15. Team events and audit
CREATE TABLE team_events (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    team_id INTEGER NOT NULL REFERENCES teams(id) ON DELETE RESTRICT,
    event_type VARCHAR(32) NOT NULL,
    actor_user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    subject_user_id INTEGER REFERENCES users(id) ON DELETE RESTRICT,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (
        event_type IN (
            'team_created',
            'team_closed',
            'team_moderated',
            'team_reopened',
            'team_deleted',
            'requirements_changed',
            'member_joined',
            'member_left',
            'member_removed'
        )
    ),
    CHECK (
        event_type NOT IN (
            'member_joined',
            'member_left',
            'member_removed'
        )
        OR subject_user_id IS NOT NULL
    )
);