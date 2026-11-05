# Scripts

Ad-hoc and migration scripts live here.

- Name scripts with a sortable prefix: `yyyyMMdd_Description.sql`.
- Make scripts idempotent where possible (`IF NOT EXISTS ...`).
- These scripts are intentionally **not** compiled into the dacpac; run them
  explicitly with `sqlcmd` or SqlPackage when needed.
- Schema changes should normally be made in `Tables/` so the dacpac stays the
  source of truth. Use this folder for data fixes, one-off backfills, and
  performance/index migrations.
