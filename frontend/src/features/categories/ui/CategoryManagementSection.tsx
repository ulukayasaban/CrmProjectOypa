import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Modal } from '../../../shared/components/Modal';
import { FieldError } from '../../../shared/components/FieldError';
import { fieldAria } from '../../../shared/lib/fieldAria';
import { Spinner } from '../../../shared/components/Spinner';
import { StateBlock } from '../../../shared/components/StateBlock';
import { PlusIcon } from '../../../shared/components/icons';
import { useToast } from '../../../shared/components/toast/ToastProvider';
import { useConfirm } from '../../../shared/hooks/useConfirm';
import { getErrorMessage } from '../../../shared/lib/errorMessage';
import {
  useCategories,
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
} from '../model/useCategories';
import {
  categorySchema,
  type CategoryFormValues,
} from '../model/categorySchema';
import type { CategoryDto } from '../../../entities/category/model/category';

/** Kategori oluştur / düzenle formu — Modal içinde kullanılır. */
interface CategoryFormModalProps {
  initial?: CategoryDto;
  onClose: () => void;
}

function CategoryFormModal({ initial, onClose }: CategoryFormModalProps) {
  const toast = useToast();
  const createCategory = useCreateCategory();
  const updateCategory = useUpdateCategory();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CategoryFormValues>({
    resolver: zodResolver(categorySchema),
    defaultValues: initial
      ? { name: initial.name, color: initial.color }
      : { name: '', color: '#3B82F6' },
  });

  const isEdit = Boolean(initial);

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEdit && initial) {
        await updateCategory.mutateAsync({ id: initial.id, payload: values });
      } else {
        await createCategory.mutateAsync(values);
      }
      onClose();
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  });

  return (
    <Modal
      title={isEdit ? 'Kategoriyi Düzenle' : 'Yeni Kategori'}
      onClose={onClose}
      width={420}
    >
      <form className="crm-form" onSubmit={onSubmit}>
        <div className="form-group">
          <label htmlFor="cat-name">Kategori Adı</label>
          <input id="cat-name" {...fieldAria('name', !!errors.name)} {...register('name')} placeholder="Örn. Öncelikli" />
          <FieldError id="name-error" message={errors.name?.message} />
        </div>

        <div className="form-group">
          <label htmlFor="cat-color-text">Renk</label>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            {/* Renk seçici — Controller ile RHF'e bağlı; fieldAria eklenmez */}
            <Controller
              name="color"
              control={control}
              render={({ field }) => (
                <input
                  id="cat-color"
                  type="color"
                  value={field.value}
                  onChange={(e) => field.onChange(e.target.value)}
                  aria-label="Renk seçici"
                  style={{
                    width: 44,
                    height: 36,
                    padding: 2,
                    borderRadius: 6,
                    border: '1px solid var(--glass-border)',
                    background: 'transparent',
                    cursor: 'pointer',
                    flexShrink: 0,
                  }}
                />
              )}
            />
            {/* Hex metin girişi — register ile bağlı */}
            <input
              id="cat-color-text"
              aria-label="Hex renk kodu"
              {...fieldAria('color', !!errors.color)}
              {...register('color')}
              placeholder="#3B82F6"
              style={{ flex: 1 }}
            />
          </div>
          <FieldError id="color-error" message={errors.color?.message} />
        </div>

        <div className="modal-footer">
          <button type="button" className="btn btn-ghost" onClick={onClose}>
            İptal
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={isSubmitting}
          >
            {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

/** ManagementPage'e eklenen kategori yönetim kartı. Sadece Admin rolü görür. */
export function CategoryManagementSection() {
  const categories = useCategories();
  const deleteCategory = useDeleteCategory();
  const toast = useToast();
  const { confirm, ConfirmEl } = useConfirm();

  const [formModal, setFormModal] = useState<
    { open: false } | { open: true; editing?: CategoryDto }
  >({ open: false });

  async function handleDelete(cat: CategoryDto) {
    const confirmed = await confirm({
      title: 'Kategoriyi Sil',
      message: `"${cat.name}" kategorisini silmek istiyor musunuz? Bu işlem geri alınamaz.`,
      confirmLabel: 'Sil',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await deleteCategory.mutateAsync(cat.id);
    } catch (err) {
      toast.error(getErrorMessage(err));
    }
  }

  return (
    <>
      <div className="glass full-width card">
        <div className="card-head">
          <h3>Kategori Yönetimi</h3>
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={() => setFormModal({ open: true, editing: undefined })}
          >
            <PlusIcon size={14} /> Yeni Kategori
          </button>
        </div>

        {categories.isLoading && <Spinner />}
        {categories.isError && (
          <StateBlock message={getErrorMessage(categories.error)} />
        )}

        {categories.data && (
          <div
            className="data-table-container"
            style={{ background: 'none', border: 'none', marginTop: 15 }}
          >
            <table className="data-table">
              <thead>
                <tr>
                  <th>Renk</th>
                  <th>Kategori Adı</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {categories.data.length === 0 && (
                  <tr>
                    <td colSpan={3} className="table-empty">
                      Henüz kategori eklenmemiş.
                    </td>
                  </tr>
                )}
                {categories.data.map((cat) => (
                  <tr key={cat.id}>
                    <td>
                      <span
                        aria-label={`Renk: ${cat.color}`}
                        style={{
                          display: 'inline-block',
                          width: 20,
                          height: 20,
                          borderRadius: 4,
                          background: cat.color,
                          border: '1px solid var(--glass-border)',
                          verticalAlign: 'middle',
                        }}
                      />
                    </td>
                    <td>
                      <span
                        className="badge"
                        style={{
                          backgroundColor: cat.color,
                          borderColor: cat.color,
                          color: '#fff',
                          fontSize: '0.78rem',
                        }}
                      >
                        {cat.name}
                      </span>
                    </td>
                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() =>
                            setFormModal({ open: true, editing: cat })
                          }
                        >
                          Düzenle
                        </button>
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          disabled={deleteCategory.isPending}
                          onClick={() => void handleDelete(cat)}
                        >
                          Sil
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {ConfirmEl}

      {formModal.open && (
        <CategoryFormModal
          initial={formModal.editing}
          onClose={() => setFormModal({ open: false })}
        />
      )}
    </>
  );
}
