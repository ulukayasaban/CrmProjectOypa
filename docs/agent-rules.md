# Agent Ekibi Süreci

Bu proje, kontrollü ve incelemeli bir geliştirme süreci için `.claude/agents/` altında tanımlı 8 rolle yürütülür. Roller hem Claude Code **subagent** olarak (tek oturumda `Agent` ile çağrılır) hem de **agent team** teammate'i olarak (deneysel) kullanılabilir.

## Roller
| Agent | Görev |
|---|---|
| **architect** | Mimari kararlar, klasör yapısı, bağımlılık kuralları |
| **backend** | .NET API, EF Core, Identity, JWT, rate limit |
| **frontend** | React, routing, state, form, API client |
| **security** | JWT, refresh token, rate limit, authorization policy |
| **database** | Entity, migration, index, ilişki tasarımı |
| **qa** | Unit/integration test, edge case, regression |
| **code-review** | Best practice, naming, SOLID, performans |
| **devops** | Docker, env, logging, CI/CD hazırlığı |

## Her büyük görev için akış
```
1. architect : Tasarımı çıkarır, dosya listesini ve mimari etkiyi verir.
2. backend / frontend : Kodu üretir.
3. security : Güvenlik kontrolü yapar.
4. qa : Test senaryolarını yazar.
5. code-review : Refactor önerir / uygular.
6. architect : Son mimari kararı verir.
```

## Çalıştırma

### Subagent olarak (tek oturum, önerilen)
Lead oturum `Agent` aracıyla rolü çağırır:
> "backend agent ile Lead modülünün Application servisini üret" → `subagent_type: backend`

Çakışmayan işler paralel başlatılabilir (ör. backend bir modül, frontend başka modül).

### Agent team olarak (deneysel)
`.claude/settings.json` içinde `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1` etkin. Lead oturuma doğal dille:
> "Bir agent team kur: bir teammate `backend` rolüyle X, biri `frontend` rolüyle Y yapsın. Plan onayı iste."

Notlar:
- Windows Terminal'de split-pane desteklenmez; in-process mod (Shift+Down ile geçiş) kullanılır.
- Teammate'ler lead'in konuşma geçmişini almaz; spawn prompt'una bağlam koy.
- Aynı dosyayı iki teammate düzenlemesin (çakışma). İş, dosya sahipliğiyle bölünür.

## Değişmez kurallar
- Kod üretmeden önce dosya listesi verilir.
- Katman bağımlılıkları, auth/güvenlik tasarımı ve incelemeli süreç en baştan sıkı tutulur.
- `oyak-crm-pro/` asla değiştirilmez (yalnızca referans).
