/**
 * Global ErrorBoundary — React render hatalarını yakalar.
 * Kullanıcıya "Bir şeyler ters gitti" + "Yeniden Dene" / "Ana Sayfa" gösterir.
 * App (router üst seviyesi) bu bileşenle sarılır.
 */
import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  errorMessage: string;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, errorMessage: '' };
  }

  static getDerivedStateFromError(error: unknown): State {
    const message =
      error instanceof Error ? error.message : 'Bilinmeyen bir hata oluştu.';
    return { hasError: true, errorMessage: message };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    // Üretimde bir loglama servisi (Sentry vb.) buraya eklenebilir
    console.error('[ErrorBoundary] Render hatası:', error, info);
  }

  handleReset = (): void => {
    this.setState({ hasError: false, errorMessage: '' });
  };

  render(): ReactNode {
    if (this.state.hasError) {
      return (
        <div className="center-screen">
          <div className="glass card" style={{ maxWidth: 480, textAlign: 'center', padding: 48 }}>
            <div style={{ fontSize: '3rem', marginBottom: 16 }}>⚠️</div>
            <h2 style={{ marginBottom: 12 }}>Bir şeyler ters gitti</h2>
            <p
              className="muted"
              style={{ fontSize: '0.9rem', marginBottom: 8, lineHeight: 1.5 }}
            >
              Beklenmeyen bir hata oluştu. Lütfen sayfayı yenileyerek tekrar
              deneyin.
            </p>
            {this.state.errorMessage && (
              <p
                className="muted"
                style={{
                  fontSize: '0.75rem',
                  marginBottom: 24,
                  fontFamily: 'monospace',
                  wordBreak: 'break-word',
                }}
              >
                {this.state.errorMessage}
              </p>
            )}
            <div style={{ display: 'flex', gap: 12, justifyContent: 'center' }}>
              <button
                type="button"
                className="btn btn-ghost"
                onClick={this.handleReset}
              >
                Yeniden Dene
              </button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={() => {
                  window.location.href = '/';
                }}
              >
                Ana Sayfa
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
