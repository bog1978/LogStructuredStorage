-- CREATE DATABASE lss_cluster;
-- Структура метаданных кластера.

DROP TABLE IF EXISTS file CASCADE;
DROP TABLE IF EXISTS bucket CASCADE;
DROP TABLE IF EXISTS node CASCADE;

-- Узел кластера
CREATE TABLE node (
  node_id VARCHAR(16) NOT NULL PRIMARY KEY, -- Имя узла кластера.
  host_name TEXT NOT NULL -- IP-адрес или DNS-имя хоста узла кластера.
);

-- Логическое хранилище - корзина (по аналогии с MinIO)
CREATE TABLE bucket (
  bucket_id VARCHAR(16) NOT NULL PRIMARY KEY, -- Имя корзины.
  node_id VARCHAR(16) NOT NULL REFERENCES node (node_id), -- ИД узела, на котором нужно размещать новые файлы.
  ttl INTERVAL NOT NULL -- Длительность хранения файлов для этой корзины.
);

-- Мапинг - на каком узле находится нужный файл.
-- На уровне кластера более детальная информация не нужна - она будет на уровне узла.
CREATE TABLE file (
  file_id BIGSERIAL PRIMARY KEY,
  file_name TEXT NOT NULL, -- Имя файла
  bucket_id VARCHAR(16) NOT NULL, -- ИД корзины
  node_id VARCHAR(16) NOT NULL REFERENCES node(node_id), -- ИД узла, на котором реально лежит файл.
  "offset" BIGINT NOT NULL, -- Смещение в файле
  part_id INT NOT NULL -- ИД части файла  
);

-- Один и тот же файл может находиться
-- либо в разных корзинах на обном узле,
-- либо в одной корзине на разных узлах (когда дело дойдет до перебалансировки).
CREATE UNIQUE INDEX unq_file_node ON file(file_name, bucket_id, node_id);