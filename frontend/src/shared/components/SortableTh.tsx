/** Tıklanabilir, asc/desc toggle destekli tablo başlık hücresi. */

interface SortableThProps {
  /** Sunucuya gönderilecek sortBy değeri. */
  field: string;
  /** Mevcut aktif sıralama sütunu. */
  activeSortBy: string;
  /** Mevcut sıralama yönü. */
  activeSortDir: 'asc' | 'desc';
  /** Sıralama değiştiğinde çağrılır. */
  onSort: (field: string, dir: 'asc' | 'desc') => void;
  children: React.ReactNode;
}

export function SortableTh({
  field,
  activeSortBy,
  activeSortDir,
  onSort,
  children,
}: SortableThProps) {
  const isActive = activeSortBy === field;
  const nextDir: 'asc' | 'desc' =
    isActive && activeSortDir === 'asc' ? 'desc' : 'asc';

  function handleClick() {
    onSort(field, nextDir);
  }

  return (
    <th
      onClick={handleClick}
      aria-sort={isActive ? (activeSortDir === 'asc' ? 'ascending' : 'descending') : 'none'}
      style={{ cursor: 'pointer', userSelect: 'none', whiteSpace: 'nowrap' }}
    >
      {children}
      {/* Aktif sütunda ok ikonu göster */}
      {isActive ? (
        <span aria-hidden="true" style={{ marginLeft: 4, opacity: 0.8 }}>
          {activeSortDir === 'asc' ? '▲' : '▼'}
        </span>
      ) : (
        <span aria-hidden="true" style={{ marginLeft: 4, opacity: 0.25 }}>
          ⇅
        </span>
      )}
    </th>
  );
}
