# MACD Sample Strategy

The strategy reproduces the MetaTrader 4 "MACD Sample" expert advisor using StockSharp's high-level API. It trades both directions on a single instrument and mirrors the original logic: take trades when the MACD line crosses its signal line on the correct side of zero while a trend EMA confirms the direction. Protective orders are converted to StockSharp's built-in risk manager with optional trailing stops.

## Trading Logic

1. Wait for at least 100 finished candles so that MACD and EMA contain enough history.
2. Calculate a standard MACD (12, 26, 9) together with its signal line and a 26-period exponential moving average that acts as a directional filter.
3. **Long entry** – allowed only when no position exists. The MACD must be below zero yet crossing above the signal line, the previous MACD value was below its signal, the absolute MACD value exceeds the configurable `MacdOpenLevel` threshold (in price points) and the trend EMA is rising.
4. **Short entry** – the symmetric setup: MACD above zero crossing below its signal, previous MACD was above the signal, the current value exceeds the `MacdOpenLevel` threshold and the trend EMA is falling.
5. **Long exit** – when MACD crosses back under the signal on the positive side of zero and the value is above `MacdCloseLevel`. The position can also be closed earlier by the trailing stop or take-profit managed by `StartProtection`.
6. **Short exit** – when MACD crosses back over the signal on the negative side and the absolute MACD value exceeds `MacdCloseLevel`, or by the protective modules.

The strategy never holds more than one position at a time. Every entry uses market orders sized by the `Volume` property. Protective logic relies on StockSharp's risk controller so take-profit distances and trailing stops remain synchronized with the instrument tick size.

## Parameters

| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `FastEmaPeriod` | Fast EMA period used by MACD. | 12 | Optimizable range 6…18.
| `SlowEmaPeriod` | Slow EMA period used by MACD. | 26 | Optimizable range 20…32.
| `SignalPeriod` | Signal EMA period within MACD. | 9 | Optimizable range 5…13.
| `TrendMaPeriod` | EMA length for the directional filter. | 26 | Optimizable range 20…40.
| `MacdOpenLevel` | Entry threshold expressed in MACD points (price steps). | 3 | Equivalent to `MACDOpenLevel` in MT4 code.
| `MacdCloseLevel` | Exit threshold expressed in MACD points. | 2 | Equivalent to `MACDCloseLevel`.
| `TakeProfitPoints` | Take profit in price points (multiplied by the instrument tick size). | 50 | Set to 0 to disable take profit.
| `TrailingStopPoints` | Trailing stop in price points. | 30 | Set to 0 to disable trailing stop.
| `CandleType` | Candle series used for indicator updates. | 5-minute time frame | Supports any StockSharp candle type.

## Implementation Notes

- The MACD and EMA indicators are bound to the candle subscription through `BindEx`/`Bind`, letting StockSharp feed ready-to-use values without manual caching.
- Positions are opened only when the platform reports `IsFormedAndOnlineAndAllowTrading()`, preventing trades while historical data is still loading or the connection is offline.
- All thresholds that refer to "points" are automatically scaled by the instrument price step, mimicking MetaTrader's `Point` constant.
- `StartProtection` converts MetaTrader's fixed take-profit and trailing stop into exchange-side protective orders. Enable or disable each module by changing the corresponding parameter.
- Extensive logging (`LogInfo`) documents each trade decision, simplifying comparison with the original expert advisor during migration validation.

## Usage Tips

- The original EA targets forex majors on intraday time frames. Start with similar symbols and adjust parameters if the instrument uses a different tick size.
- When testing symbols with exotic tick values, verify `Security.PriceStep` is configured; otherwise the default of 1.0 will be used.
- Combine with StockSharp's portfolio protection features if you need account-level money management beyond per-position stops.

## Tags

- Trend following
- Momentum
- MACD crossover
- Intraday (default 5-minute)
- Trailing stop + take profit
