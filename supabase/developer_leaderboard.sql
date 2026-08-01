create table if not exists public.developer_profiles (
  developer_id uuid primary key,
  display_name text not null default 'Entwickler',
  total_experience integer not null default 0 check (total_experience >= 0),
  current_level integer not null default 1 check (current_level >= 1),
  completed_games integer not null default 0 check (completed_games >= 0),
  wins integer not null default 0 check (wins >= 0),
  unlocked_achievement_ids jsonb not null default '[]'::jsonb,
  updated_at timestamptz not null default timezone('utc', now())
);

alter table public.developer_profiles enable row level security;

grant usage on schema public to anon;
grant select, insert, update on table public.developer_profiles to anon;

drop policy if exists developer_profiles_read on public.developer_profiles;
create policy developer_profiles_read
on public.developer_profiles
for select
to anon
using (true);

drop policy if exists developer_profiles_insert on public.developer_profiles;
create policy developer_profiles_insert
on public.developer_profiles
for insert
to anon
with check (true);

drop policy if exists developer_profiles_update on public.developer_profiles;
create policy developer_profiles_update
on public.developer_profiles
for update
to anon
using (true)
with check (true);
