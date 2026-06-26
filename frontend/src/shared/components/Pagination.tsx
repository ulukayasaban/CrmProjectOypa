/** Tablo sayfaları için yeniden kullanılabilir sayfalama bileşeni. */

interface PaginationProps {
  /** Mevcut sayfa numarası (1 tabanlı). */
  page: number;
  /** Toplam sayfa sayısı. */
  totalPages: number;
  /** Toplam kayıt sayısı. */
  totalCount: number;
  /** Sayfa başına gösterilecek kayıt sayısı. */
  pageSize: number;
  /** Sayfa değiştiğinde çağrılır. */
  onPageChange: (page: number) => void;
  /** Sayfa boyutu değiştiğinde çağrılır. */
  onPageSizeChange: (pageSize: number) => void;
}

const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;

export function Pagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
}: PaginationProps) {
  const hasPrev = page > 1;
  const hasNext = page < totalPages;

  // Gösterim aralığı: örn. "21-40 / 83 kayıt"
  const startItem = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItem = Math.min(page * pageSize, totalCount);

  return (
    <div
      className="pagination-bar"
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: 10,
        padding: '10px 0',
        fontSize: '0.85rem',
      }}
    >
      {/* Sol: kayıt bilgisi */}
      <span className="muted">
        {totalCount === 0
          ? 'Kayıt yok'
          : `${startItem}–${endItem} / ${totalCount} kayıt`}
      </span>

      {/* Orta: önceki/sonraki + sayfa göstergesi */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <button
          type="button"
          className="btn btn-ghost btn-sm"
          disabled={!hasPrev}
          aria-label="Önceki sayfa"
          onClick={() => onPageChange(page - 1)}
        >
          ‹ Önceki
        </button>

        <span style={{ padding: '0 8px', fontWeight: 500 }}>
          {page} / {totalPages === 0 ? 1 : totalPages}
        </span>

        <button
          type="button"
          className="btn btn-ghost btn-sm"
          disabled={!hasNext}
          aria-label="Sonraki sayfa"
          onClick={() => onPageChange(page + 1)}
        >
          Sonraki ›
        </button>
      </div>

      {/* Sağ: sayfa boyutu seçici */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <span className="muted">Sayfa başına:</span>
        <select
          value={pageSize}
          aria-label="Sayfa başına kayıt sayısı"
          onChange={(event) => {
            onPageSizeChange(Number(event.target.value));
          }}
          style={{ width: 70 }}
        >
          {PAGE_SIZE_OPTIONS.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
