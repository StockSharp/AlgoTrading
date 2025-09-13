# Symr New Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Symr New Bar Strategy** demonstrates how to detect the beginning of new candles across multiple timeframes using a single subscription. The strategy monitors a base timeframe and calculates when larger intervals such as 5m, 15m, 30m, 1h, 4h, 1d, 20m and 55m begin. Each detected bar is logged.

## Details

- **Entry Criteria**: None. The strategy does not place trades.
- **Exit Criteria**: None.
- **Long/Short**: Not applicable.
- **Stops**: No stops are used.

### Parameters

| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Base timeframe for new bar detection. |

### Notes

- Stores the last open time for each predefined period.
- When the base period advances, larger periods are evaluated and logged if they roll over.
- Useful as a template for multi-timeframe event handling.
