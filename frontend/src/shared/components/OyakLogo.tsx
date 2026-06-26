interface OyakLogoProps {
  /** Logo yüksekliği (px); genişlik orana göre ölçeklenir. */
  height?: number;
}

/**
 * OYAK Pazarlama logosu — SVG.
 * Koyu tema uyarlaması: marka ögeleri (elips + alt çizgi) OYAK kırmızısı,
 * "OYAK"/"PAZARLAMA" sözcük markası koyu zeminde okunsun diye beyaz.
 * Birebir kurumsal raster istenirse frontend/public/ altına konup buranın
 * yerine <img> kullanılabilir.
 */
export function OyakLogo({ height = 34 }: OyakLogoProps) {
  return (
    <svg
      height={height}
      viewBox="0 0 270 60"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      role="img"
      aria-label="OYAK Pazarlama"
    >
      {/* Kırmızı elips halka */}
      <ellipse
        cx="47"
        cy="29"
        rx="44"
        ry="25"
        stroke="var(--oyak-red, #e30613)"
        strokeWidth="6"
        fill="none"
      />
      {/* Alt swoosh (kuyruk) */}
      <path
        d="M9 41 Q47 62 90 36"
        stroke="var(--oyak-red, #e30613)"
        strokeWidth="5"
        fill="none"
        strokeLinecap="round"
      />
      {/* OYAK */}
      <text
        x="47"
        y="37"
        textAnchor="middle"
        fontFamily="Inter, sans-serif"
        fontWeight="800"
        fontSize="23"
        letterSpacing="0.5"
        fill="#ffffff"
      >
        OYAK
      </text>
      {/* PAZARLAMA */}
      <text
        x="104"
        y="34"
        fontFamily="Inter, sans-serif"
        fontWeight="800"
        fontSize="20"
        letterSpacing="0.5"
        fill="#ffffff"
      >
        PAZARLAMA
      </text>
      {/* Kırmızı alt çizgi */}
      <rect
        x="104"
        y="41"
        width="156"
        height="5"
        rx="2"
        fill="var(--oyak-red, #e30613)"
      />
    </svg>
  );
}
