import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios';
import type { ApiEnvelope } from '../types/api';
import { ApiError } from '../types/api';
import { tokenStorage } from './tokenStorage';

const baseURL = import.meta.env.VITE_API_BASE_URL;

/**
 * Endpoints that must never trigger the 401 refresh flow, otherwise a failed
 * refresh would recurse infinitely.
 */
const AUTH_BYPASS_PATHS = ['/auth/login', '/auth/refresh'];

interface RetriableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

interface RawAuthResponse {
  accessToken: string;
}

/**
 * Called when the session can no longer be recovered (refresh failed/expired).
 * Wired up by the auth provider so it can clear React state and redirect.
 */
let onSessionExpired: (() => void) | null = null;

export function setSessionExpiredHandler(handler: () => void): void {
  onSessionExpired = handler;
}

export const httpClient: AxiosInstance = axios.create({
  baseURL,
  // Refresh token HttpOnly çerezde taşındığından, isteklerde çerezin gönderilmesi
  // için withCredentials zorunlu (CORS tarafında AllowCredentials + sabit origin var).
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

httpClient.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken();
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }
  return config;
});

function toApiError(error: AxiosError<ApiEnvelope<unknown>>): ApiError {
  const envelope = error.response?.data;
  const message =
    envelope?.message ?? error.message ?? 'Beklenmeyen bir hata oluştu.';
  const errors = envelope?.errors ?? [];
  return new ApiError(message, errors, error.response?.status);
}

/**
 * Ensures only a single refresh request is in flight; concurrent 401s await it.
 */
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  // Refresh token gövdede DEĞİL; HttpOnly çerezde. withCredentials ile çerez
  // otomatik gönderilir. Çerez yoksa/süresi dolmuşsa sunucu 401 döner.
  const response = await axios.post<ApiEnvelope<RawAuthResponse>>(
    `${baseURL}/auth/refresh`,
    undefined,
    { withCredentials: true, headers: { 'Content-Type': 'application/json' } },
  );

  const envelope = response.data;
  if (!envelope.success) {
    throw new ApiError(envelope.message, envelope.errors, 401);
  }

  tokenStorage.setAccessToken(envelope.data.accessToken);
  return envelope.data.accessToken;
}

httpClient.interceptors.response.use(
  (response: AxiosResponse<ApiEnvelope<unknown>>) => {
    // Blob responses are raw file downloads — skip envelope unwrapping.
    if (response.config.responseType === 'blob') {
      return response;
    }
    const envelope = response.data;
    if (envelope && typeof envelope === 'object' && 'success' in envelope) {
      if (!envelope.success) {
        throw new ApiError(envelope.message, envelope.errors, response.status);
      }
      // Unwrap the envelope so callers receive `data` directly.
      response.data = envelope.data as never;
    }
    return response;
  },
  async (error: AxiosError<ApiEnvelope<unknown>>) => {
    if (error instanceof ApiError) {
      return Promise.reject(error);
    }

    const config = error.config as RetriableConfig | undefined;
    const status = error.response?.status;
    const url = config?.url ?? '';
    const isAuthBypass = AUTH_BYPASS_PATHS.some((path) => url.includes(path));

    if (status === 401 && config && !config._retry && !isAuthBypass) {
      config._retry = true;
      try {
        refreshPromise = refreshPromise ?? refreshAccessToken();
        const newToken = await refreshPromise;
        refreshPromise = null;
        config.headers.set('Authorization', `Bearer ${newToken}`);
        return httpClient(config);
      } catch {
        refreshPromise = null;
        tokenStorage.clear();
        onSessionExpired?.();
        return Promise.reject(toApiError(error));
      }
    }

    return Promise.reject(toApiError(error));
  },
);
