# Estratégia Binary Wave StdDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que soma sinais de MA, MACD, CCI, Momentum, RSI e ADX usando pesos configuráveis.
Opera na direção da pontuação acumulada quando a volatilidade medida pelo desvio padrão ultrapassa um limiar.
Stop loss e take profit opcionais em pontos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: pontuação > 0 e StdDev >= EntryVolatility
  - Vendido: pontuação < 0 e StdDev >= EntryVolatility
- **Critérios de saída**:
  - A volatilidade cai abaixo de ExitVolatility
- **Stops**: Opcional através de `UseStopLoss` e `UseTakeProfit`
- **Valores padrão**:
  - `WeightMa` = 1
  - `WeightMacd` = 1
  - `WeightCci` = 1
  - `WeightMomentum` = 1
  - `WeightRsi` = 1
  - `WeightAdx` = 1
  - `MaPeriod` = 13
  - `FastMacd` = 12
  - `SlowMacd` = 26
  - `SignalMacd` = 9
  - `CciPeriod` = 14
  - `MomentumPeriod` = 14
  - `RsiPeriod` = 14
  - `AdxPeriod` = 14
  - `StdDevPeriod` = 9
  - `EntryVolatility` = 1.5
  - `ExitVolatility` = 1
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MA, MACD, CCI, Momentum, RSI, ADX, StandardDeviation
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
