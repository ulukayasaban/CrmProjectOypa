import { useEffect, useState } from 'react';

/**
 * Verilen değeri belirtilen gecikme (ms) sonra günceller.
 * Arama inputlarında sunucuya aşırı istek gitmesini önler.
 */
export function useDebouncedValue<T>(value: T, delay = 300): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(timer);
    };
  }, [value, delay]);

  return debouncedValue;
}
