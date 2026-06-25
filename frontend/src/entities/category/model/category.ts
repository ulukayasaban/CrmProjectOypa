/** Kategori DTO — backend sözleşmesine birebir karşılık gelir. */
export interface CategoryDto {
  id: string;
  name: string;
  /** Hex renk kodu — örnek: "#D4AF37" */
  color: string;
}
