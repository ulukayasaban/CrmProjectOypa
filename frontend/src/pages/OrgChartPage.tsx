import { useEmployees } from '../features/org/model/useEmployees';
import type { EmployeeDto } from '../entities/employee/model/employee';
import { Spinner } from '../shared/components/Spinner';
import { StateBlock } from '../shared/components/StateBlock';
import { getErrorMessage } from '../shared/lib/errorMessage';

interface OrgNode extends EmployeeDto {
  children: OrgNode[];
}

function buildTree(employees: EmployeeDto[]): OrgNode[] {
  const map = new Map<string, OrgNode>();

  for (const emp of employees) {
    map.set(emp.id, { ...emp, children: [] });
  }

  const roots: OrgNode[] = [];

  for (const node of map.values()) {
    if (node.managerId === null) {
      roots.push(node);
    } else {
      const parent = map.get(node.managerId);
      if (parent) {
        parent.children.push(node);
      } else {
        // manager not found in list — treat as root
        roots.push(node);
      }
    }
  }

  return roots;
}

function roleBadge(emp: EmployeeDto): React.ReactNode {
  if (!emp.hasAccount) {
    return (
      <span
        className="badge"
        style={{
          background: 'rgba(255,255,255,0.08)',
          color: 'rgba(255,255,255,0.4)',
          border: '1px solid rgba(255,255,255,0.12)',
        }}
      >
        Pozisyon
      </span>
    );
  }
  if (emp.role === 'Admin') {
    return (
      <span
        className="badge"
        style={{
          background: 'rgba(227,6,19,0.18)',
          color: 'var(--accent-gold)',
          border: '1px solid rgba(227,6,19,0.35)',
        }}
      >
        Yönetici
      </span>
    );
  }
  if (emp.role === 'Sales') {
    return (
      <span
        className="badge"
        style={{
          background: 'rgba(59,130,246,0.15)',
          color: '#93c5fd',
          border: '1px solid rgba(59,130,246,0.3)',
        }}
      >
        Satış
      </span>
    );
  }
  return null;
}

interface OrgNodeCardProps {
  node: OrgNode;
  depth: number;
}

function OrgNodeCard({ node, depth }: OrgNodeCardProps) {
  const isRoot = depth === 0;

  return (
    <div className={isRoot ? 'org-node' : 'org-node org-node--nested'}>
      <div
        className="glass org-card"
        style={{
          background: isRoot
            ? 'rgba(227,6,19,0.08)'
            : 'rgba(255,255,255,0.04)',
          borderTop: isRoot ? '2px solid var(--accent-gold)' : undefined,
        }}
      >
        <div
          style={{
            width: 36,
            height: 36,
            borderRadius: '50%',
            background: isRoot
              ? 'rgba(227,6,19,0.22)'
              : 'rgba(255,255,255,0.08)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0,
            fontSize: '0.85rem',
            fontWeight: 700,
            color: isRoot ? 'var(--accent-gold)' : 'rgba(255,255,255,0.5)',
          }}
        >
          {node.fullName ? node.fullName.charAt(0).toUpperCase() : '?'}
        </div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
            <strong style={{ fontSize: '0.8rem', color: 'var(--accent-gold)' }}>
              {node.title}
            </strong>
            {roleBadge(node)}
          </div>
          <div
            style={{
              fontSize: '0.9rem',
              fontWeight: 600,
              color: node.fullName ? 'var(--text-primary)' : 'rgba(255,255,255,0.35)',
              marginTop: 2,
            }}
          >
            {node.fullName ?? '— (boş pozisyon)'}
          </div>
          {node.email && (
            <div
              className="muted"
              style={{ fontSize: '0.75rem', marginTop: 2 }}
            >
              {node.email}
            </div>
          )}
        </div>
      </div>

      {node.children.length > 0 && (
        <div style={{ marginTop: 0 }}>
          {node.children.map((child) => (
            <OrgNodeCard key={child.id} node={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

export default function OrgChartPage() {
  const { data, isLoading, isError, error } = useEmployees();

  if (isLoading) return <Spinner />;
  if (isError) return <StateBlock message={getErrorMessage(error)} />;
  if (!data || data.length === 0) {
    return <StateBlock message="Henüz organizasyon verisi bulunamadı." />;
  }

  const roots = buildTree(data);

  return (
    <>
      <div className="page-head">
        <div>
          <h3>Organizasyon Şeması</h3>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            Ekip hiyerarşisini ve pozisyonlara bağlı hesapları görüntüleyin.
          </p>
        </div>
      </div>

      <div className="glass card" style={{ padding: 24 }}>
        <div>
          {roots.map((root) => (
            <OrgNodeCard key={root.id} node={root} depth={0} />
          ))}
        </div>
      </div>
    </>
  );
}
