# Estratégia Liquidex Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Liquidex Keltner** opera rompimentos dos Canais Keltner com um filtro de tendência por média móvel.
As operações são permitidas somente durante as horas especificadas e podem ser confirmadas opcionalmente pela direção do RSI.
Stop-loss e take-profit são gerenciados mediante percentuais fixos.

## Detalhes
- **Critérios de entrada**:
  - O preço cruza para acima da banda superior Keltner e fecha acima da média móvel.
  - O preço cruza para abaixo da banda inferior Keltner e fecha abaixo da média móvel.
  - O corpo da vela deve superar `RangeFilter`.
  - Quando `UseRsiFilter` está habilitado, o RSI deve estar acima de 50 para comprados e abaixo de 50 para vendidos.
  - O horário atual deve estar entre `EntryHourFrom` e `EntryHourTo`, e antes de `FridayEndHour` nas sextas-feiras.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Sim, baseados em percentual via `StartProtection`.
- **Valores padrão**:
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MA, Keltner, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
