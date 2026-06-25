/**
 * TableSkeleton
 * Liste sayfalarında veri yüklenirken tam-sayfa spinner yerine tablo iskeleti gösterir.
 * Sayfa chrome'u (arama/filtre/sekme) görünür kalır; yalnızca tablo gövdesi iskelete döner.
 */
interface TableSkeletonProps {
  /** Tablo sütun sayısı (gerçek tabloyla aynı olmalı). */
  columns: number;
  /** İskelet satır sayısı (varsayılan 6). */
  rows?: number;
}

/** Her hücre için biraz değişken genişlik — tek tip blok yerine doğal görünüm. */
const CELL_WIDTHS = ['80%', '60%', '70%', '50%', '85%', '65%'];

export function TableSkeleton({ columns, rows = 6 }: TableSkeletonProps) {
  return (
    <div
      className="data-table-container glass"
      aria-busy="true"
      aria-label="Yükleniyor"
    >
      <table className="data-table">
        <tbody>
          {Array.from({ length: rows }).map((_, rowIndex) => (
            <tr key={rowIndex}>
              {Array.from({ length: columns }).map((_, colIndex) => (
                <td key={colIndex}>
                  <div
                    className="skeleton skeleton-text"
                    style={{
                      width: CELL_WIDTHS[(rowIndex + colIndex) % CELL_WIDTHS.length],
                    }}
                  />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
