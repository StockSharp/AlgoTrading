# Syndicate Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp translation of the original MetaTrader script **Syndicate_Trader_v_1_04.mq4** from folder `MQL/12351`.

It trades based on a crossover between fast and slow exponential moving averages with a volume spike confirmation. Optional session filters restrict trading to specific hours. Simple take profit and stop loss levels manage risk.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA and volume exceeds the moving average multiplied by a configurable factor.
  - **Short**: Fast EMA crosses below slow EMA with the same volume confirmation.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite crossover.
  - Stop loss or take profit hit.
  - Outside allowed session window.
- **Stops**: Fixed stop loss and take profit in price points.
- **Filters**:
  - Volume spike filter.
  - Optional session time filter.

## Parameters

| Name | Description |
|------|-------------|
| `FastEmaLength` | Period of the fast EMA. |
| `SlowEmaLength` | Period of the slow EMA. |
| `VolumeMaLength` | Period for averaging volume. |
| `VolumeMultiplier` | Multiplier applied to average volume to define a spike. |
| `TakeProfitPoints` | Take profit in price points. |
| `StopLossPoints` | Stop loss in price points. |
| `UseSessionFilter` | Enable or disable the session filter. |
| `SessionStartHour/SessionStartMinute` | Start time of trading session. |
| `SessionEndHour/SessionEndMinute` | End time of trading session. |
| `CandleType` | Candle type and timeframe. |

