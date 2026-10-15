# Swing Multi-Indicador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de swing combinando Parabolic SAR, SuperTrend, ADX e confirmação por delta de volume.

## Detalhes

- **Critérios de entrada**: Todos os indicadores habilitados concordam.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou atingir stop-loss/take-profit.
- **Stops**: Níveis percentuais opcionais.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: PSAR, SuperTrend, ADX, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (2m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
