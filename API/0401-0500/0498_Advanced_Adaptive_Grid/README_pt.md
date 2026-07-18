# Estratégia de Grade Adaptativa Avançada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Grade Adaptativa Avançada utiliza múltiplos indicadores técnicos para avaliar a direção da tendência e constrói uma grade dinâmica de níveis de entrada. O tamanho da grade se adapta à volatilidade via ATR e as ordens são colocadas quando o preço toca os níveis da grade na direção da tendência. Os controles de risco incluem stop-loss fixo, take-profit, trailing stop, saída baseada em tempo e limite de perda diária.

## Detalhes

- **Critérios de entrada**:
  - Em mercados com tendência: preço atingindo níveis de grade calculados com confirmação do RSI.
  - Em mercados laterais: RSI sobrecomprado/sobrevendido dispara entradas na grade.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss, take-profit, trailing stop, reversão de tendência ou saída por tempo.
- **Stops**: Fixo e trailing.
- **Valores padrão**:
  - `BaseGridSize` = 1
  - `MaxPositions` = 5
  - `UseVolatilityGrid` = True
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `ShortMaLength` = 20
  - `LongMaLength` = 50
  - `SuperLongMaLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `UseTrailingStop` = True
  - `TrailingStopPercent` = 1
  - `MaxLossPerDay` = 5
  - `TimeBasedExit` = True
  - `MaxHoldingPeriod` = 48
- **Filtros**:
  - Categoria: Grade / Tendência
  - Direção: Ambos
  - Indicadores: ATR, SMA, MACD, RSI, Momentum
  - Stops: Sim
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
