# ZeroLag MACD Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on a crossover between the MACD line and its signal line. It was converted from the MetaTrader expert advisor **ZeroLagEA-AIP v0.0.4**. The strategy operates only during the configured session hours and can optionally require that the crossover happens on the current bar.

## Details

- **Entry Criteria**:
  - **Long**: MACD line crosses above the signal line.
  - **Short**: MACD line crosses below the signal line.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover or forced exit at the specified day and hour.
- **Stops**: None.
- **Filters**:
  - Session hours defined by `StartHour` and `EndHour`.
  - Optional fresh crossover requirement (`UseFreshSignal`).

## Parameters

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = 1-minute candles
