# Basic RSI EA Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Basic RSI EA Template Strategy** replicates the MetaTrader 4 expert advisor "Basic Rsi EA Template.mq4" (MQL/26750). It watches the Relative Strength Index (RSI) on the selected candle series and reacts when momentum stretches into configurable overbought or oversold zones. The StockSharp conversion keeps the simple one-position workflow and the protective stop/take logic of the original robot while adopting the high-level subscription API.

## Strategy Logic

### Indicators
- **Relative Strength Index (RSI)** with a configurable period calculated on the chosen candle type.

### Entry Conditions
- **Long setup**: when RSI falls below `OversoldLevel` and the strategy has no open position, it sends a market buy order for the configured `OrderVolume`.
- **Short setup**: when RSI rises above `OverboughtLevel` and the strategy has no open position, it sends a market sell order for the configured `OrderVolume`.

The algorithm works in a netting mode: only one position can exist at any time. If a long position is active the strategy waits for it to close before a short entry (and vice versa).

### Exit Conditions
- **Protective stop**: `StopLossPips` converts into an absolute price distance using the instrument tick size. Once the price retraces by that amount the built-in protection engine closes the position.
- **Take profit**: `TakeProfitPips` is processed the same way—when price moves in favor by the configured distance the position is closed for profit.

There is no additional trailing or signal-based exit. The strategy relies purely on the protective distances or manual intervention to exit trades, mirroring the minimalist design of the original template.

### Risk and Volume Handling
- `OrderVolume` defines the fixed amount submitted with every market order (default 0.01 lots, matching the MQL sample).
- The strategy does not pyramid nor hedge. If a protective stop or take-profit closes the active trade the algorithm becomes flat and waits for the next RSI trigger.

## Parameters
- `CandleType`: candle series used for signal generation (default: 1-minute time frame).
- `RsiPeriod`: number of bars in the RSI window (default: 14).
- `OverboughtLevel`: RSI threshold that allows short entries (default: 70).
- `OversoldLevel`: RSI threshold that allows long entries (default: 30).
- `StopLossPips`: stop distance in pips converted to absolute price units (default: 30 pips).
- `TakeProfitPips`: profit target in pips converted to absolute price units (default: 20 pips).
- `OrderVolume`: fixed volume for market orders (default: 0.01).

## Implementation Notes
- Uses `SubscribeCandles(...).Bind(rsi, ProcessCandle)` so indicator values flow directly into the processing method without manual buffer management.
- `CreateProtectionUnit` recreates the MQL pip handling: instruments with 3 or 5 decimals use a 10x multiplier to map pips to price steps.
- All indicator checks run on finished candles to avoid multiple orders on the same bar.
- The conversion assumes a netting account, unlike MetaTrader's hedging mode. Consequently, opposite trades close the current position instead of creating multiple tickets.
- Inline comments and logs are in English to help future maintenance.

## Usage Tips
- Adjust `CandleType` to the instrument and timeframe you wish to trade (e.g., switch to hourly candles for swing setups).
- Tune `StopLossPips` and `TakeProfitPips` so they match the instrument volatility; the protective distances are essential for risk control.
- Combine the strategy with StockSharp portfolio or risk modules if you need advanced money management beyond the template logic.
