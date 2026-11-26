BEGIN;
INSERT INTO users (email, full_name, password_hash, show_in_recommendations)
VALUES ('artyom@example.com', 'Артём', 'art123', TRUE)
ON CONFLICT (lower(email)) DO UPDATE
SET show_in_recommendations = EXCLUDED.show_in_recommendations
WHERE NOT users.show_in_recommendations;
DO $$
DECLARE captain INTEGER;
demo RECORD;
category INTEGER;
required_skills INTEGER [];
team_id INTEGER;
BEGIN
SELECT id INTO captain
FROM users
WHERE lower(email) = 'artyom@example.com';
FOR demo IN
SELECT *
FROM (
        VALUES (
                'Commit Together',
                'Платформа для разработчиков, где можно найти напарника для учёбы и вместе создавать небольшие проекты. Помогите нам разработать веб-интерфейс.',
                'Фронтенд-разработчик',
                'Клиентская разработка',
                ARRAY ['JavaScript', 'HTML', 'CSS']
            ),
            (
                'Campus API',
                'Открытый каталог учебных аудиторий, мероприятий и студенческих клубов. Мы создаём надёжный API с понятной документацией.',
                'Бэкенд-разработчик',
                'Серверная разработка',
                ARRAY ['C#', '.NET', 'PostgreSQL']
            ),
            (
                'Pixel Notes',
                'Удобное пространство для заметок, набросков и совместных идей. Ищем человека, которому нравится создавать продуманные интерфейсы.',
                'UI/UX-дизайнер',
                'UX/UI Дизайнер',
                ARRAY ['Figma', 'UI/UX']
            ),
            (
                'Data Trail',
                'Превращаем данные об общественном транспорте в полезные выводы. Присоединяйтесь, чтобы исследовать данные и создавать понятные графики.',
                'Аналитик данных',
                'Аналитик данных',
                ARRAY ['Python', 'pandas', 'SQL']
            ),
            (
                'Ship It',
                'Простая панель управления релизами студенческих проектов. Помогите нам подключить инструменты развёртывания и упростить отслеживание релизов.',
                'DevOps-инженер',
                'DevOps',
                ARRAY ['Docker', 'Linux', 'Git']
            ),
            (
                'Open Shelf',
                'Общественная библиотека для обмена книгами и рекомендациями. Ищем фронтенд-разработчика, который поможет запустить первую версию.',
                'React-разработчик',
                'Клиентская разработка',
                ARRAY ['React', 'TypeScript', 'CSS']
            ),
            (
                'Quiet Campus',
                'Карта тихих мест для учёбы. Хотим добавить фильтры по розеткам, времени работы и доступности аудиторий.',
                'Фронтенд-разработчик',
                'Клиентская разработка',
                ARRAY ['JavaScript', 'HTML', 'CSS']
            ),
            (
                'Lab Booking',
                'Сервис бронирования лабораторий. Ищем разработчика для расписания и проверки пересекающихся заявок.',
                'Бэкенд-разработчик',
                'Серверная разработка',
                ARRAY ['C#', '.NET', 'PostgreSQL']
            ),
            (
                'Museum Route',
                'Конструктор маршрутов по небольшим музеям. Нужны макеты каталога и карточек экспонатов.',
                'UI/UX-дизайнер',
                'UX/UI Дизайнер',
                ARRAY ['Figma', 'UI/UX']
            ),
            (
                'Weather Archive',
                'Сравниваем исторические данные о погоде в разных городах и публикуем сезонные графики.',
                'Аналитик данных',
                'Аналитик данных',
                ARRAY ['Python', 'pandas', 'SQL']
            ),
            (
                'Preview Box',
                'Поднимаем временные среды для демонстрации изменений в студенческих проектах.',
                'DevOps-инженер',
                'DevOps',
                ARRAY ['Docker', 'Linux', 'Git']
            ),
            (
                'Volunteer Board',
                'Доска волонтёрских задач для местных сообществ. Проектируем каталог событий и форму записи.',
                'React-разработчик',
                'Клиентская разработка',
                ARRAY ['React', 'TypeScript', 'CSS']
            ),
            (
                'Course Compass',
                'Собираем учебные курсы и помогаем студентам составить личный план на семестр.',
                'Фронтенд-разработчик',
                'Клиентская разработка',
                ARRAY ['JavaScript', 'HTML', 'CSS']
            ),
            (
                'Tool Library',
                'Сервис учёта инструментов в общей мастерской. Нужен API для выдачи, возврата и бронирования.',
                'Бэкенд-разработчик',
                'Серверная разработка',
                ARRAY ['C#', '.NET', 'PostgreSQL']
            ),
            (
                'First Week',
                'Интерактивный путеводитель для первокурсников. Ищем дизайнера для карты кампуса и списка дел на первую неделю.',
                'UI/UX-дизайнер',
                'UX/UI Дизайнер',
                ARRAY ['Figma', 'UI/UX']
            ),
            (
                'Bike Count',
                'Изучаем загруженность велодорожек по данным городских счётчиков и сравниваем районы.',
                'Аналитик данных',
                'Аналитик данных',
                ARRAY ['Python', 'pandas', 'SQL']
            ),
            (
                'Log Garden',
                'Собираем логи учебных сервисов в одном месте, чтобы командам было проще разбирать сбои.',
                'DevOps-инженер',
                'DevOps',
                ARRAY ['Docker', 'Linux', 'Git']
            ),
            (
                'Recipe Circle',
                'Каталог недорогих рецептов с поиском по продуктам. Ищем разработчика для фильтров и списка покупок.',
                'React-разработчик',
                'Клиентская разработка',
                ARRAY ['React', 'TypeScript', 'CSS']
            ),
            (
                'Studio Queue',
                'Веб-очередь для консультаций в учебной студии. Нужен интерфейс записи и отображения текущего места.',
                'Фронтенд-разработчик',
                'Клиентская разработка',
                ARRAY ['JavaScript', 'HTML', 'CSS']
            ),
            (
                'Lost and Found',
                'Каталог найденных вещей в кампусе. Делаем API для публикации объявлений и отметок о возврате.',
                'Бэкенд-разработчик',
                'Серверная разработка',
                ARRAY ['C#', '.NET', 'PostgreSQL']
            ),
            (
                'Green Courtyard',
                'Сервис для участников дворовых садов. Ищем дизайнера для карточек растений и календаря ухода.',
                'UI/UX-дизайнер',
                'UX/UI Дизайнер',
                ARRAY ['Figma', 'UI/UX']
            ),
            (
                'Library Pulse',
                'Анализируем посещаемость библиотек, чтобы показать, когда в читальных залах больше свободных мест.',
                'Аналитик данных',
                'Аналитик данных',
                ARRAY ['Python', 'pandas', 'SQL']
            ),
            (
                'Backup Booth',
                'Учебный проект по резервному копированию. Настраиваем расписание и проверку восстановления файлов.',
                'DevOps-инженер',
                'DevOps',
                ARRAY ['Docker', 'Linux', 'Git']
            ),
            (
                'Practice Room',
                'Площадка для совместной подготовки к собеседованиям. Делаем интерфейс поиска партнёра и выбора времени.',
                'React-разработчик',
                'Клиентская разработка',
                ARRAY ['React', 'TypeScript', 'CSS']
            ),
            (
                'Local Layers',
                'Карта городских прогулок с заметками об архитектуре. Нужен удобный просмотр маршрута с телефона.',
                'Фронтенд-разработчик',
                'Клиентская разработка',
                ARRAY ['JavaScript', 'HTML', 'CSS']
            ),
            (
                'Workshop Pass',
                'Система записи на мастер-классы. Ищем разработчика для списка ожидания и учёта свободных мест.',
                'Бэкенд-разработчик',
                'Серверная разработка',
                ARRAY ['C#', '.NET', 'PostgreSQL']
            )
    ) AS examples(name, description, title, category_name, skills) LOOP IF EXISTS (
        SELECT 1
        FROM teams
        WHERE captain_id = captain
            AND name = demo.name
    ) THEN CONTINUE;
END IF;
SELECT id INTO category
FROM position_categories
WHERE name = demo.category_name;
SELECT array_agg(id) INTO required_skills
FROM skills
WHERE name = ANY(demo.skills);
IF category IS NULL
OR cardinality(required_skills) IS DISTINCT
FROM cardinality(demo.skills) THEN RAISE EXCEPTION 'Run the skill catalog seed before the team seed.';
END IF;
team_id := public.create_team(
    captain,
    demo.name,
    demo.description,
    demo.title,
    category,
    required_skills
);
IF team_id IS NULL THEN RAISE EXCEPTION 'The team seed requires active skills and position categories.';
END IF;
END LOOP;
END;
$$;
COMMIT;
