# Arttrader Strategy

## Overview
Arttrader is a conversion of the MetaTrader 4 expert advisor `Arttrader_v1_5`. The system operates on hourly candles and attempts to capture smooth directional moves measured by an exponential moving average (EMA) of the open price. Entries are filtered by both the EMA slope and a strict intrabar price position check, while a dedicated volatility guard blocks trades after large opening gaps. Positions are managed through a timed stop-loss procedure, fixed emergency stop and take-profit levels, and a volume-based fail-safe.

The StockSharp port keeps the original inputs and executes trades through high-level market orders. All calculations are performed on finished candles; intrabar timing requirements from the expert advisor are approximated by comparing the configured minute delays against the candle duration.

## Strategy logic
### Indicator
* **Open-price EMA** – a single EMA with configurable period (`EMA Speed`) is calculated on the candle open price. The difference between the current and previous EMA values defines the slope in pips.

### Filters
* **Slope bounds** – the EMA slope must lie between the minimum (`Slope Min`) and maximum (`Slope Max`) thresholds. The strategy ignores trades when the trend is either too weak or too strong.
* **Intrabar alignment** – long trades require the candle to close below or equal to its open and remain within the low plus the configured entry slip. Short trades mirror the condition around the high. The delay parameters (`Entry Delay`, `Exit Delay`) are satisfied when the current candle’s duration is at least as long as the configured minutes.
* **Volatility spike guard** – evaluates open-to-open differences across the latest five candles. If any single-gap exceeds `Big Jump` pips, or any two-bar gap exceeds `Double Jump` pips, new entries are blocked for the current bar.

### Entries
* **Long entry** – triggered when all filters pass, the EMA slope is positive, and there is no existing position. The stored synthetic entry price is adjusted by the `Spread Adjust` parameter to emulate the original spread compensation.
* **Short entry** – symmetric logic that requires a negative EMA slope and no active position.

### Exits
* **Timed smart stop** – once in profit or loss, the strategy evaluates the smart stop only after the `Exit Delay` requirement is satisfied. For longs it demands the close to be above the open and sufficiently close to the high, while the loss in pips relative to the synthetic entry price must exceed `Smart Stop`.
* **Volume fail-safe** – if the previously completed candle volume is less than or equal to `Min Volume`, any open position is closed immediately on the next bar.
* **Emergency stop / take profit** – as soon as a trade opens, a hard emergency stop and a fixed take-profit level are recorded. If the candle range reaches either level, the position is closed without waiting for the timed filters.

## Parameters
* **Order Volume** – trade size used for market orders.
* **EMA Period** – length of the EMA applied to candle opens.
* **Big Jump (pips)** – largest allowed single-bar opening gap before entry signals are suppressed.
* **Double Jump (pips)** – largest allowed two-bar opening gap before entry signals are suppressed.
* **Smart Stop (pips)** – pip distance required to trigger the timed stop-loss logic.
* **Emergency Stop (pips)** – hard stop distance evaluated on every candle high/low.
* **Take Profit (pips)** – fixed take-profit distance evaluated on every candle high/low.
* **Slope Min / Slope Max (pips)** – EMA slope bounds for trade eligibility.
* **Entry Delay (min)** – minimum candle duration (in minutes) before entries are permitted.
* **Exit Delay (min)** – minimum candle duration (in minutes) before the timed stop can execute.
* **Entry Slip / Exit Slip (pips)** – tolerance between the close and the extreme when validating entry and exit filters.
* **Min Volume** – minimum previous candle volume; trades are closed if the value is not exceeded.
* **Spread Adjust (pips)** – synthetic spread compensation applied to the stored entry price.
* **Slippage (pips)** – informational setting preserved for compatibility with the MetaTrader inputs.
* **Candle Type** – timeframe used for candle subscriptions (defaults to 1-hour candles).

## Notes
* The StockSharp implementation executes market orders and clears positions using `BuyMarket`/`SellMarket`, matching the single-position behaviour of the original EA.
* Because StockSharp operates on finished candles, the intrabar minute checks from MetaTrader are approximated by comparing the configured delays to the candle’s total duration.
* The emergency stop and take-profit levels are evaluated against candle highs and lows, emulating the broker-side protective orders from the MetaTrader version.
