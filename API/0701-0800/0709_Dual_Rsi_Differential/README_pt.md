# Diferencial RSI Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Diferencial RSI Duplo compara dois períodos de RSI e opera quando a diferença entre eles cruza um limiar. Esta abordagem de duplo período busca capturar divergências entre o momentum de curto e longo prazo.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`.
  - **Vendido**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`.
- **Critérios de saída**: Limiar oposto, período de manutenção opcional, take profit/stop loss opcionais.
- **Stops**: Take profit e stop loss opcionais (`Condition`).
- **Valores padrão**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado e vendido
  - Indicadores: RSI
  - Complexidade: Básico
  - Nível de risco: Médio
