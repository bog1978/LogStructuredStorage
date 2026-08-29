ALTER TABLE node
  RENAME COLUMN node_id TO node_name;

ALTER TABLE node
  ADD COLUMN node_id SMALLSERIAL NOT NULL;

ALTER TABLE bucket
  RENAME COLUMN bucket_id TO bucket_name;

ALTER TABLE bucket
  ADD COLUMN bucket_id SERIAL NOT NULL;