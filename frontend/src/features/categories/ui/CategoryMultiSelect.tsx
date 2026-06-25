import type { CategoryDto } from '../../../entities/category/model/category';

interface CategoryMultiSelectProps {
  categories: CategoryDto[];
  value: string[];
  onChange: (ids: string[]) => void;
  /** Erişilebilirlik: grubu tanımlayan id */
  groupLabelId?: string;
}

/**
 * Tüm kategorileri erişilebilir checkbox rozet listesi olarak gösterir.
 * Seçili id'leri `value` dizisi ile kontrol eder; seçim değişince `onChange`
 * güncel id dizisini döndürür.
 */
export function CategoryMultiSelect({
  categories,
  value,
  onChange,
  groupLabelId,
}: CategoryMultiSelectProps) {
  function toggle(id: string) {
    if (value.includes(id)) {
      onChange(value.filter((v) => v !== id));
    } else {
      onChange([...value, id]);
    }
  }

  if (categories.length === 0) {
    return (
      <p className="muted" style={{ fontSize: '0.85rem' }}>
        Henüz kategori tanımlanmamış.
      </p>
    );
  }

  return (
    <div
      role="group"
      aria-labelledby={groupLabelId}
      style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}
    >
      {categories.map((cat) => {
        const checked = value.includes(cat.id);
        return (
          <label
            key={cat.id}
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 6,
              cursor: 'pointer',
              padding: '4px 10px',
              borderRadius: 6,
              border: `2px solid ${cat.color}`,
              background: checked ? cat.color : 'transparent',
              color: checked ? '#fff' : 'var(--text-main)',
              fontSize: '0.82rem',
              fontWeight: 500,
              transition: 'all 0.18s ease',
              userSelect: 'none',
            }}
          >
            <input
              type="checkbox"
              checked={checked}
              onChange={() => toggle(cat.id)}
              aria-label={cat.name}
              style={{ position: 'absolute', opacity: 0, width: 0, height: 0 }}
            />
            <span
              style={{
                display: 'inline-block',
                width: 10,
                height: 10,
                borderRadius: '50%',
                background: cat.color,
                border: checked ? '2px solid rgba(255,255,255,0.6)' : `2px solid ${cat.color}`,
                flexShrink: 0,
              }}
              aria-hidden="true"
            />
            {cat.name}
          </label>
        );
      })}
    </div>
  );
}
