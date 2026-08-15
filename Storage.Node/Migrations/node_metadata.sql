-- CREATE DATABASE lss_node;
-- Структура метаданных узла.

DROP TABLE IF EXISTS package CASCADE;
DROP TABLE IF EXISTS file CASCADE;

-- Пакетный файл, в который будут складываться файлы поменьше.
CREATE TABLE package (
  package_id BIGSERIAL PRIMARY KEY,
  file_path TEXT NOT NULL,
  bucket_id VARCHAR(16) NOT NULL,
  size BIGINT NOT NULL,
  write_offset BIGINT NOT NULL,
  is_closed BOOLEAN NOT NULL
);

-- Пользовательский файл - содержимое пакетного файла.
CREATE TABLE file (
  file_id BIGSERIAL PRIMARY KEY,
  file_name TEXT NOT NULL,
  package_id BIGINT NOT NULL REFERENCES package (package_id), -- ИД пакетного файла
  file_size BIGINT NOT NULL, -- Размер файла
  file_offset BIGINT NOT NULL, -- Смещение в пакетном файле
  created_at TIMESTAMPTZ NOT NULL DEFAULT (CURRENT_TIMESTAMP) -- Дата и время создания файла
);