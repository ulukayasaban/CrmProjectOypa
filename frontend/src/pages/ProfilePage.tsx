import { useQuery } from '@tanstack/react-query';
import { authApi } from '../features/auth/api/authApi';
import { queryKeys } from '../shared/api/queryKeys';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { getErrorMessage } from '../shared/lib/errorMessage';

export default function ProfilePage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: queryKeys.me,
    queryFn: authApi.me,
  });

  if (isLoading) return <Spinner />;
  if (isError || !data) return <StateBlock message={getErrorMessage(error)} />;

  return (
    <div className="glass profile-card">
      <div className="avatar profile-avatar">{data.fullName[0]}</div>
      <h2>{data.fullName}</h2>
      <p style={{ color: 'var(--accent-gold)', marginBottom: 20 }}>
        {data.position ?? data.roles.join(', ')}
      </p>
      <div className="profile-info">
        <span>
          📧 <strong>E-posta:</strong> {data.email}
        </span>
        <span>
          📞 <strong>Telefon:</strong> {data.phone ?? '-'}
        </span>
        <span>
          🛡️ <strong>Roller:</strong> {data.roles.join(', ') || '-'}
        </span>
      </div>
    </div>
  );
}
