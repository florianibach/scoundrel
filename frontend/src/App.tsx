import { useQuery } from '@tanstack/react-query';

type Profile = { id: number; name: string; avatarUrl?: string };
type Ruleset = { id: number; name: string; description?: string };
type LeaderboardEntry = { profileId: number; profileName: string; bestScore: number; sessionsPlayed: number; totalScore: number };

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`);
  if (!response.ok) {
    throw new Error(`Request failed for ${path}`);
  }
  return response.json() as Promise<T>;
}

export function App() {
  const profilesQuery = useQuery({ queryKey: ['profiles'], queryFn: () => getJson<Profile[]>('/api/profiles') });
  const rulesetsQuery = useQuery({ queryKey: ['rulesets'], queryFn: () => getJson<Ruleset[]>('/api/rulesets') });
  const leaderboardQuery = useQuery({ queryKey: ['leaderboard'], queryFn: () => getJson<LeaderboardEntry[]>('/api/leaderboard') });

  return (
    <main className="container">
      <header>
        <p className="eyebrow">Scoundrel · R1 Basic Setup</p>
        <h1>Mobile-first Dashboard</h1>
        <p>React + TypeScript + Vite frontend connected to ASP.NET Core API with SQLite.</p>
      </header>

      <section className="grid">
        <article className="card">
          <h2>Profiles</h2>
          <ul>
            {profilesQuery.data?.map((profile) => (
              <li key={profile.id}>{profile.name}</li>
            ))}
          </ul>
        </article>

        <article className="card">
          <h2>Rulesets</h2>
          <ul>
            {rulesetsQuery.data?.map((ruleset) => (
              <li key={ruleset.id}>{ruleset.name}</li>
            ))}
          </ul>
        </article>

        <article className="card wide">
          <h2>Leaderboard</h2>
          <div className="tableWrap">
            <table>
              <thead>
                <tr>
                  <th>Player</th>
                  <th>Best</th>
                  <th>Sessions</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {leaderboardQuery.data?.map((entry) => (
                  <tr key={entry.profileId}>
                    <td>{entry.profileName}</td>
                    <td>{entry.bestScore}</td>
                    <td>{entry.sessionsPlayed}</td>
                    <td>{entry.totalScore}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      </section>
    </main>
  );
}
