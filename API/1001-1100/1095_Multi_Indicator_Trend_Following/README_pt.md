# Estratégia de Seguimento de Tendência Multi-Indicador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA com confirmação de RSI e volume. Utiliza stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**: A EMA rápida cruza acima/abaixo da EMA lenta com filtro RSI e volume elevado
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss e take profit baseados em ATR
- **Stops**: Sim, baseados em ATR
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `FastMaLength` = 10
  - `SlowMaLength` = 30
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `AtrPeriod` = 14
  - `StopLossAtrMultiplier` = 2
  - `TakeProfitAtrMultiplier` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, RSI, ATR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
