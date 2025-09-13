# MA2CCI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a fast and slow Simple Moving Average (SMA) crossover with the Commodity Channel Index (CCI) as a confirmation filter. A position is opened only when both the moving averages and CCI cross their levels in the same direction. The Average True Range (ATR) defines the initial stop-loss distance.

The system can trade in both directions. There is no take-profit; positions are closed on an opposite signal or when the ATR-based stop-loss is triggered.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above Slow SMA **and** CCI crosses above 0.
  - **Short**: Fast SMA crosses below Slow SMA **and** CCI crosses below 0.
- **Exit Criteria**:
  - Opposite SMA crossover.
  - ATR-based stop-loss.
- **Indicators**: SMA, CCI, ATR.
- **Timeframe**: Configurable via `CandleType`.
- **Default Parameters**:
  - `Fast MA Period` = 4
  - `Slow MA Period` = 8
  - `CCI Period` = 4
  - `ATR Period` = 4
- **Long/Short**: Both.
- **Stops**: Yes, dynamic stop using ATR.
