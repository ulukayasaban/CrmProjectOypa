import type { CategoryDto } from '../../../entities/category/model/category';

interface CategoryBadgesProps {
  categories: CategoryDto[];
}

/**
 * Hex renk değerinden arka plana göre okunabilir ön plan rengi hesaplar.
 * W3C parlaklık formülüne göre: koyu arka plan → beyaz metin, açık → siyah.
 */
function getContrastColor(hex: string): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  // ITU-R BT.601 luma
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? '#1a1a1a' : '#ffffff';
}

export function CategoryBadges({ categories }: CategoryBadgesProps) {
  if (categories.length === 0) {
    return <span className="muted">—</span>;
  }

  return (
    <span style={{ display: 'inline-flex', flexWrap: 'wrap', gap: 4 }}>
      {categories.map((cat) => (
        <span
          key={cat.id}
          className="badge"
          style={{
            backgroundColor: cat.color,
            borderColor: cat.color,
            color: getContrastColor(cat.color),
            fontSize: '0.72rem',
            padding: '2px 8px',
          }}
        >
          {cat.name}
        </span>
      ))}
    </span>
  );
}
