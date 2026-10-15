# Long EMA com Saída Avançada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Long EMA com Saída Avançada é uma estratégia somente de compra que entra quando uma média móvel curta cruza acima de uma média móvel média e o preço está acima de uma média móvel longa. As saídas podem ser acionadas por cruzamento descendente do MACD, fechamento do preço abaixo de uma média móvel selecionada, cruzamento descendente da MA, stop trailing ou um filtro de volatilidade baseado em ATR.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A MA curta cruza acima da MA média e o preço está acima da MA longa.
- **Critérios de saída**: Cruzamento descendente do MACD, preço abaixo da MA selecionada, cruzamento descendente da MA curta abaixo da MA média, stop trailing opcional.
- **Stops**: Stop trailing opcional.
- **Valores padrão**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: MA, MACD, ATR
  - Complexidade: Médio
  - Nível de risco: Médio
