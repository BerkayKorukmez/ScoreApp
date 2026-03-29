-- Veritabanı tabloları EnsureCreated / initializer ile oluşturulmuş,
-- __EFMigrationsHistory boşsa ilk migration tekrar CREATE dener ve hata verir.
-- Bu script o iki migration'ı "uygulandı" olarak işaretler; sonra sadece yeni migration çalışır.
--
-- Kullanım (psql veya pgAdmin):
--   psql -h localhost -U postgres -d SkorTakipDB -f baseline-ef-migrations.sql
--
-- Ardından:
--   dotnet ef database update

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20260313193309_AddMatchMediaAndLeagueFields', '8.0.11'),
('20260322123048_AddAiChatMessages', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;
