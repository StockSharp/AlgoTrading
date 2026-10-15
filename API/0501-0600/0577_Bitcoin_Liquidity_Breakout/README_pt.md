# Estratégia de Rompimento de Liquidez do Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em posições compradas quando a liquidez e a volatilidade são altas e a tendência de curto prazo é altista. Alta liquidez é definida como volume acima de sua média móvel multiplicado por um limiar. A volatilidade é confirmada quando o ATR supera sua média móvel.

## Detalhes

- **Critérios de entrada**:
  - `Volume > SMA(volume) * LiquidityThreshold`
  - `Variação de preço (%) > PriceChangeThreshold`
  - `SMA rápida > SMA lenta`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: SMA rápida cruzando abaixo da SMA lenta ou RSI > 70.
- **Stops**: Percentuais opcionais de stop-loss e take profit.
- **Valores padrão**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: SMA, RSI, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: 1h
