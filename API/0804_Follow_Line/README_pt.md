# Estratégia de Linha de Seguimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia rastreia uma linha de seguimento derivada de rompimentos das Bandas de Bollinger com offset de ATR opcional. As entradas ocorrem quando a linha muda de direção, opcionalmente confirmada pela tendência de um período superior.

## Detalhes

- **Critérios de entrada**: A linha de seguimento muda de direção após o preço romper as Bandas de Bollinger com confirmação opcional do período superior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: A linha de seguimento ou a tendência do período superior se reverte.
- **Stops**: Não.
- **Valores padrão**:
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
