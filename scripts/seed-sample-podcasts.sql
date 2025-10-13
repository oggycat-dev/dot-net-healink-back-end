-- =====================================================
-- SEED 5 SAMPLE PODCASTS FOR TESTING RECOMMENDATION
-- Creator: datdanh0301@gmail.com (4a426ae0-34b4-4be8-8007-6b70fe37b314)
-- =====================================================

-- Variables
DO $$
DECLARE
    v_creator_id UUID := '4a426ae0-34b4-4be8-8007-6b70fe37b314';
    v_podcast_1 UUID;
    v_podcast_2 UUID;
    v_podcast_3 UUID;
    v_podcast_4 UUID;
    v_podcast_5 UUID;
BEGIN
    -- Generate UUIDs for podcasts
    v_podcast_1 := gen_random_uuid();
    v_podcast_2 := gen_random_uuid();
    v_podcast_3 := gen_random_uuid();
    v_podcast_4 := gen_random_uuid();
    v_podcast_5 := gen_random_uuid();
    
    RAISE NOTICE '🎧 Creating 5 sample podcasts...';
    
    -- ==========================================
    -- Podcast 1: Thiền Định và Tâm Thức
    -- ==========================================
    INSERT INTO "Contents" (
        "Id", "Title", "Description", "ContentType", "ContentStatus",
        "Discriminator", "Tags", "EmotionCategories", "TopicCategories",
        "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt",
        "Duration", "AudioUrl", "ThumbnailUrl", "TranscriptUrl",
        "HostName", "GuestName", "EpisodeNumber", "SeriesName",
        "ViewCount", "LikeCount", "ShareCount", "CommentCount", "Status"
    ) VALUES (
        v_podcast_1,
        'Thiền Định và Tâm Thức - Hành Trình Khám Phá Bên Trong',
        'Khám phá sức mạnh của thiền định trong việc cải thiện sức khỏe tinh thần. Học cách kiểm soát cảm xúc, giảm stress và sống tỉnh thức hơn mỗi ngày. Podcast này sẽ hướng dẫn bạn các kỹ thuật thiền cơ bản và nâng cao.',
        1,  -- ContentType: Podcast
        5,  -- ContentStatus: Published
        'Podcast',
        '["thiền", "mindfulness", "sức khỏe tinh thần", "meditation", "wellness"]',
        '[1, 3]',  -- Calm, Happy
        '[2, 5]',  -- Health, Mindfulness
        v_creator_id,
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days',
        NOW() - INTERVAL '4 days',
        '00:25:30',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio/sample-thien-dinh.mp3',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/thumbnails/sample-thien-dinh.png',
        'Transcript về thiền định và tâm thức...',
        'Tuấn Đạt',
        'Thầy Minh Niệm',
        1,
        'Sức Khỏe Tinh Thần',
        120, 45, 10, 8, 1
    );
    
    -- ==========================================
    -- Podcast 2: Quản Lý Thời Gian Hiệu Quả
    -- ==========================================
    INSERT INTO "Contents" (
        "Id", "Title", "Description", "ContentType", "ContentStatus",
        "Discriminator", "Tags", "EmotionCategories", "TopicCategories",
        "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt",
        "Duration", "AudioUrl", "ThumbnailUrl", "TranscriptUrl",
        "HostName", "GuestName", "EpisodeNumber", "SeriesName",
        "ViewCount", "LikeCount", "ShareCount", "CommentCount", "Status"
    ) VALUES (
        v_podcast_2,
        'Quản Lý Thời Gian - Bí Quyết Làm Chủ Cuộc Đời',
        'Bạn có cảm thấy 24 giờ một ngày không đủ? Podcast này chia sẻ các phương pháp quản lý thời gian hiệu quả như Pomodoro, Time Blocking, và cách ưu tiên công việc theo ma trận Eisenhower.',
        1, 5, 'Podcast',
        '["quản lý thời gian", "productivity", "hiệu suất", "time management", "work-life balance"]',
        '[2, 4]', '[1, 3]',
        v_creator_id,
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days',
        NOW() - INTERVAL '3 days',
        '00:32:15',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio/sample-time-management.mp3',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/thumbnails/sample-time-management.png',
        'Transcript về quản lý thời gian...',
        'Tuấn Đạt', 'Tony Dzung', 2, 'Phát Triển Bản Thân',
        250, 89, 15, 12, 1
    );
    
    -- ==========================================
    -- Podcast 3: Vượt Qua Cảm Giác Lo Âu
    -- ==========================================
    INSERT INTO "Contents" (
        "Id", "Title", "Description", "ContentType", "ContentStatus",
        "Discriminator", "Tags", "EmotionCategories", "TopicCategories",
        "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt",
        "Duration", "AudioUrl", "ThumbnailUrl", "TranscriptUrl",
        "HostName", "GuestName", "EpisodeNumber", "SeriesName",
        "ViewCount", "LikeCount", "ShareCount", "CommentCount", "Status"
    ) VALUES (
        v_podcast_3,
        'Vượt Qua Lo Âu - Tìm Lại Bình An Nội Tâm',
        'Lo âu là cảm giác phổ biến trong cuộc sống hiện đại. Podcast này giúp bạn hiểu rõ nguồn gốc của lo âu, các kỹ thuật breathing, grounding và CBT để đối phó.',
        1, 5, 'Podcast',
        '["lo âu", "anxiety", "tâm lý", "mental health", "stress relief"]',
        '[1, 5]', '[2, 4]',
        v_creator_id,
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days',
        NOW() - INTERVAL '2 days',
        '00:28:45',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio/sample-anxiety.mp3',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/thumbnails/sample-anxiety.png',
        'Transcript về vượt qua lo âu...',
        'Tuấn Đạt', 'Bác Sĩ Minh Anh', 3, 'Sức Khỏe Tinh Thần',
        310, 156, 25, 18, 1
    );
    
    -- ==========================================
    -- Podcast 4: Kỹ Năng Giao Tiếp Thuyết Phục
    -- ==========================================
    INSERT INTO "Contents" (
        "Id", "Title", "Description", "ContentType", "ContentStatus",
        "Discriminator", "Tags", "EmotionCategories", "TopicCategories",
        "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt",
        "Duration", "AudioUrl", "ThumbnailUrl", "TranscriptUrl",
        "HostName", "GuestName", "EpisodeNumber", "SeriesName",
        "ViewCount", "LikeCount", "ShareCount", "CommentCount", "Status"
    ) VALUES (
        v_podcast_4,
        'Giao Tiếp Thuyết Phục - Nghệ Thuật Kết Nối Con Người',
        'Làm sao để thuyết phục người khác mà không gây áp lực? Học các nguyên tắc từ cuốn Influence của Robert Cialdini, kỹ thuật lắng nghe tích cực.',
        1, 5, 'Podcast',
        '["giao tiếp", "communication", "leadership", "soft skills", "persuasion"]',
        '[2, 4]', '[1, 3]',
        v_creator_id,
        NOW() - INTERVAL '1 day',
        NOW() - INTERVAL '1 day',
        NOW() - INTERVAL '1 day',
        '00:35:20',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio/sample-communication.mp3',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/thumbnails/sample-communication.png',
        'Transcript về giao tiếp...',
        'Tuấn Đạt', 'Coach Hà Anh', 4, 'Kỹ Năng Mềm',
        180, 72, 20, 14, 1
    );
    
    -- ==========================================
    -- Podcast 5: Yoga và Sức Khỏe Thể Chất
    -- ==========================================
    INSERT INTO "Contents" (
        "Id", "Title", "Description", "ContentType", "ContentStatus",
        "Discriminator", "Tags", "EmotionCategories", "TopicCategories",
        "CreatedBy", "CreatedAt", "UpdatedAt", "PublishedAt",
        "Duration", "AudioUrl", "ThumbnailUrl", "TranscriptUrl",
        "HostName", "GuestName", "EpisodeNumber", "SeriesName",
        "ViewCount", "LikeCount", "ShareCount", "CommentCount", "Status"
    ) VALUES (
        v_podcast_5,
        'Yoga Cơ Bản - Kết Nối Thân Và Tâm',
        'Yoga không chỉ là bài tập thể dục mà còn là hành trình kết nối giữa thân thể và tâm trí. Giới thiệu các tư thế yoga cơ bản cho người mới bắt đầu.',
        1, 5, 'Podcast',
        '["yoga", "sức khỏe", "fitness", "wellness", "thể dục"]',
        '[1, 3]', '[2, 6]',
        v_creator_id,
        NOW() - INTERVAL '5 hours',
        NOW() - INTERVAL '5 hours',
        NOW() - INTERVAL '5 hours',
        '00:30:00',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/audio/sample-yoga.mp3',
        'https://healink-upload-file.s3.ap-southeast-2.amazonaws.com/podcasts/thumbnails/sample-yoga.png',
        'Transcript về yoga...',
        'Tuấn Đạt', 'Cô Lan Phương', 5, 'Sống Khỏe',
        95, 38, 8, 5, 1
    );
    
    RAISE NOTICE '✅ Successfully created 5 sample podcasts!';
    RAISE NOTICE '📊 Podcast IDs:';
    RAISE NOTICE '  1. Thiền Định: %', v_podcast_1;
    RAISE NOTICE '  2. Quản Lý Thời Gian: %', v_podcast_2;
    RAISE NOTICE '  3. Vượt Qua Lo Âu: %', v_podcast_3;
    RAISE NOTICE '  4. Giao Tiếp: %', v_podcast_4;
    RAISE NOTICE '  5. Yoga: %', v_podcast_5;
    
END $$;

-- Verify
SELECT 
    "Id",
    "Title",
    "ContentType",
    "Duration",
    "ViewCount",
    "LikeCount",
    "PublishedAt"
FROM "Contents"
WHERE "CreatedBy" = '4a426ae0-34b4-4be8-8007-6b70fe37b314'
  AND "Discriminator" = 'Podcast'
ORDER BY "PublishedAt" DESC;
