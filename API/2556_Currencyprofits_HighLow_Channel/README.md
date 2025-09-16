# Currencyprofits High-Low Channel Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor `Currencyprofits_01.1`. It combines a fast/slow moving average trend filter with a breakout of the recent channel extreme. When the fast moving average is above the slow average, the strategy expects a bullish environment and waits for price to retest the lowest low of the previous channel window. Short trades are taken when the fast average is below the slow average and price retests the highest high of the channel.

The implementation works on any instrument that provides candle data. All calculations are performed on closed candles to ensure stability in both backtests and live trading.

## Trading logic
1. Subscribe to the configured candle type and compute two moving averages and a Donchian-style channel based on the previous `ChannelLength` candles (default 6 bars).
2. Store the previous candle values from the indicators to mimic the original MQL logic that uses a one-bar shift.
3. **Long entry**: when the previous fast MA is greater than the previous slow MA and the current candle low touches or breaks the previous channel low.
4. **Short entry**: when the previous fast MA is less than the previous slow MA and the current candle high touches or breaks the previous channel high.
5. **Exit rules**:
   - Close long positions if the next candle closes above the stored channel high or if the protective stop is hit.
   - Close short positions if the next candle closes below the stored channel low or if the protective stop is hit.
6. Only one position is active at a time; the strategy ignores new signals while a trade is open.

## Position sizing
- `RiskPercent` defines the fraction of the portfolio value that can be risked per trade (default `0.14`, i.e., 14%).
- The stop-loss distance is derived from `StopLossPoints` multiplied by the security `PriceStep` (or points if no metadata is available).
- Cash risk per contract is estimated with the exchange step value (`StepPrice`). If the security does not expose this information, the raw price distance is used instead.
- The final order volume is aligned to the instrument trading constraints (`VolumeStep`, `MinVolume`, `MaxVolume`). If risk-based sizing cannot be calculated, the base `Volume` of the strategy is used.

## Parameters
- `FastLength` – length of the fast moving average used to detect the trend (default 32).
- `FastMaType` – type of the fast moving average (Simple, Exponential, Smoothed, Weighted).
- `SlowLength` – length of the slow moving average (default 86).
- `SlowMaType` – type of the slow moving average.
- `PriceSource` – candle price applied to both moving averages (default Close).
- `ChannelLength` – number of previous candles that form the high/low channel (default 6).
- `StopLossPoints` – stop distance expressed in instrument points before it is converted to a price (default 170).
- `RiskPercent` – fraction of equity risked per trade (default 0.14 → 14%).
- `CandleType` – timeframe of the candles used for all calculations (default 1 hour, can be changed to match the desired chart period).

## Usage notes
- Ensure `Security.PriceStep`, `Security.StepPrice`, and volume metadata are filled for accurate position sizing.
- Set the strategy `Volume` to a sensible fallback value when risk-based sizing is disabled (e.g., `RiskPercent = 0`).
- The logic trades on closed candles; live executions occur on the bar close that confirms the signal.
- The stop-loss is managed internally; there is no separate take-profit, mirroring the source expert advisor.

## Source
Converted from `MQL/17641/Currencyprofits_01.1.mq5` with emphasis on readability and compatibility with the high-level StockSharp API.
