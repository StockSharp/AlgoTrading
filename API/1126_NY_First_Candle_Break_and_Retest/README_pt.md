# Estratégia de Rompimento e Reteste da Primeira Vela NY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera Rompimentos da primeira vela da sessão de Nova York com confirmação de reteste. Usa ATR para posicionamento de stops e alvos de risco-retorno com filtro de tendência EMA opcional e trailing stop.

## Detalhes

- **Critérios de entrada**: Rompimento da máxima ou mínima da primeira vela de sessão seguido de um reteste dentro de `RetestThreshold` ATR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop baseado em ATR e alvo `RewardRiskRatio`. Trailing stop opcional.
- **Stops**: `AtrMultiplier` * ATR.
- **Valores padrão**:
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, EMA
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
