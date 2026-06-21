# Rompimento Megabar (Range e Volume e RSI)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Megabar Breakout detecta velas grandes sustentadas por alto volume e confirmação do RSI. A estratégia entra comprado em megabares altistas e vendido em baixistas.

Multiplica o range e volume médios para encontrar megabares. A média móvel do RSI filtra as operações.

## Detalhes

- **Critérios de entrada**: O corpo da vela e o volume superam suas médias móveis pelos multiplicadores definidos. RSI MA acima do limiar de compra e abaixo do limiar de venda.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss ou take profit.
- **Stops**: Sim.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VolumeAveragePeriod` = 20
  - `VolumeMultiplier` = 3
  - `RangeAveragePeriod` = 20
  - `RangeMultiplier` = 4
  - `RsiPeriod` = 14
  - `RsiMaPeriod` = 14
  - `LongRsiThreshold` = 50
  - `ShortRsiThreshold` = 70
  - `TakeProfit` = 400
  - `StopLoss` = 300
  - `FilterTradeHours` = false
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Volume, Range, RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
