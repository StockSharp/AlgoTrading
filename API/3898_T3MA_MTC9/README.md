# T3MA(MTC) Strategy

Converted from the MetaTrader 4 expert advisor **T3MA(MTC).mq4** (directory `MQL/7904`). The original robot trades signals from the "T3MA-ALARM" indicator: it builds a double-smoothed exponential moving average and places an order whenever the slope of that curve flips from falling to rising or vice versa. The StockSharp port mirrors the same logic with idiomatic high-level APIs.

## Trading idea

1. Build a first EMA using the selected candle type and period.
2. Smooth that series with a second EMA of the same period.
3. Compare the smoothed value with the previous one (optionally shifted by `MaShift`).
4. When the slope changes direction, the strategy records a signal. Orders are executed after the configured `CalculationBarOffset` delay, reproducing the `CalculationBarIndex` parameter of the EA.
5. Each signal uses the bar's low (for a long entry) or high (for a short entry) as a unique marker to avoid duplicate trades, just like the `LastOrder` variable in MetaTrader.

## Porting details

- Uses two `ExponentialMovingAverage` instances to emulate the T3MA-ALARM smoothing chain.
- Maintains a tiny queue of recent smoothed values to support the `MaShift` lookback.
- Signals are stored in a FIFO queue and executed after the requested number of finished candles.
- Protective orders are managed through `StartProtection` with distances expressed in price steps, matching MetaTrader points.
- The `AllowMultiplePositions` flag reproduces the `MultiPositions` input: when disabled, the strategy waits until the net position is flat before acting on a new signal.

## Parameters

- `MaPeriod` – EMA length used for both smoothing passes (default: 4).
- `MaShift` – number of bars to shift the smoothed series before comparing its slope (default: 0).
- `CalculationBarOffset` – delay (in finished candles) between detecting a signal and sending the order (default: 1).
- `TradeVolume` – base order volume in lots (default: 1).
- `UseStopLoss` / `StopLossPoints` – enable and distance of the stop loss in price steps (default: enabled, 40 steps).
- `UseTakeProfit` / `TakeProfitPoints` – enable and distance of the take profit in price steps (default: enabled, 11 steps).
- `AllowMultiplePositions` – allow stacking positions even when an opposite one is open (default: enabled).
- `CandleType` – timeframe or data type used to feed the indicator chain (default: 5-minute candles).

## Trading workflow

1. Subscribe to the chosen candle series and feed closing prices through the double EMA chain.
2. Track the current slope direction and generate a signal when it flips.
3. Push each signal (or the absence of one) into the delay queue so that executions happen exactly after `CalculationBarOffset` completed candles, just like the MQL4 script reads older indicator buffers.
4. When a matured signal is executed:
   - Skip it if trading is disabled, the platform is not ready, or `AllowMultiplePositions` is off while a net position is already open.
   - Ensure the signal marker differs from the previous one to prevent duplicates.
   - Send a market order (`BuyMarket`/`SellMarket`) with the configured volume. Protective stops are attached automatically when enabled.

## Notes

- Price comparisons use a small decimal tolerance to avoid floating-point artifacts when checking the `LastOrder` analogue.
- The strategy does not auto-close opposite positions when `AllowMultiplePositions` is disabled, mimicking the original EA that relied on protective exits.
- Visualization of candles and own trades is available when the charting subsystem is present.
