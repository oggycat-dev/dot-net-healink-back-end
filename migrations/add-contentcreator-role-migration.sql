-- Migration: Add ContentCreator role to AuthService
-- Run this in authservicedb database

-- Step 1: Add ContentCreator role if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM "Roles" WHERE "Name" = 'ContentCreator') THEN
        INSERT INTO "Roles" ("Id", "Name", "NormalizedName", "Status", "ConcurrencyStamp")
        VALUES (gen_random_uuid(), 'ContentCreator', 'CONTENTCREATOR', 1, gen_random_uuid()::text);
        
        RAISE NOTICE 'ContentCreator role added successfully';
    ELSE
        RAISE NOTICE 'ContentCreator role already exists';
    END IF;
END $$;

-- Step 2: Verify role was created
SELECT "Id", "Name", "NormalizedName", "Status" 
FROM "Roles" 
WHERE "Name" = 'ContentCreator';

-- Step 3 (Optional): Add ContentCreator role to existing approved creators
-- This is for users who were approved BEFORE this fix was deployed
-- Uncomment the following if you want to migrate existing creators:

/*
DO $$
DECLARE
    v_creator_role_id uuid;
    v_user_role_id uuid;
    v_affected_users int := 0;
BEGIN
    -- Get role IDs
    SELECT "Id" INTO v_creator_role_id FROM "Roles" WHERE "Name" = 'ContentCreator';
    SELECT "Id" INTO v_user_role_id FROM "Roles" WHERE "Name" = 'User';
    
    -- Add ContentCreator role to users who:
    -- 1. Have User role in AuthService
    -- 2. Have approved CreatorApplication in UserService
    -- 3. Don't already have ContentCreator role
    
    WITH approved_creators AS (
        -- Query UserService to get approved creator user IDs
        -- You need to adjust this based on your UserService database
        SELECT DISTINCT ca."UserId"
        FROM userservicedb."CreatorApplications" ca
        WHERE ca."Status" = 2  -- Approved status
    )
    INSERT INTO "UserRoles" ("UserId", "RoleId")
    SELECT ac."UserId", v_creator_role_id
    FROM approved_creators ac
    WHERE EXISTS (
        -- User exists in AuthService
        SELECT 1 FROM "Users" u WHERE u."Id" = ac."UserId"
    )
    AND NOT EXISTS (
        -- Doesn't already have ContentCreator role
        SELECT 1 FROM "UserRoles" ur 
        WHERE ur."UserId" = ac."UserId" AND ur."RoleId" = v_creator_role_id
    );
    
    GET DIAGNOSTICS v_affected_users = ROW_COUNT;
    RAISE NOTICE 'Added ContentCreator role to % existing users', v_affected_users;
END $$;
*/

-- Step 4: Verify final state
SELECT 
    COUNT(*) as total_content_creators,
    COUNT(DISTINCT ur."UserId") as unique_users
FROM "UserRoles" ur
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE r."Name" = 'ContentCreator';
