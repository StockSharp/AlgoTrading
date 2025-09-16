# Color JFATL Digit TM Strategy

## Overview
The **Color JFATL Digit TM Strategy** is a port of the original MetaTrader 5 expert advisor that combines a Jurik-filtered FATL (Fast Adaptive Trend Line) with color-based state transitions and an optional trading session filter. The strategy monitors the slope of the smoothed FATL line: each bar is classified as bullish (color = 2), bearish (color = 0) or neutral (color = 1). Changes in these color states trigger entries, exits and position management while respecting configurable session hours, stop-loss and take-profit distances.

## Logic
1. **Custom indicator replication**
   - The FATL value is calculated by convolving the selected applied price with the original weight table of 39 coefficients.
   - The result is smoothed using StockSharp's `JurikMovingAverage`. If the library exposes a `Phase` property it is configured via reflection to mirror the MT5 inputs.
   - The smoothed value is rounded to instrument precision by multiplying the price step by `10^DigitRounding`, reproducing the `Digit` parameter from MQL5.
   - The difference between the current rounded value and the previous one defines the color for the bar (`2 = rising`, `0 = falling`, `1 = unchanged / inherited`).

2. **Signal evaluation**
   - A ring buffer keeps the most recent color codes. The `SignalBar` parameter selects how many completed bars to skip (default = 1, i.e., previous closed bar).
   - A **long entry** is triggered when the preceding color was bullish (`2`) and the most recent color is anything other than bullish (`< 2`).
   - A **short entry** is triggered when the preceding color was bearish (`0`) and the most recent color is anything other than bearish (`> 0`).
   - A **long exit** occurs whenever the preceding color becomes bearish (`0`).
   - A **short exit** occurs whenever the preceding color becomes bullish (`2`).
   - Entries are skipped when a position already exists, replicating the single-position behaviour of the MT5 expert.

3. **Session control and protection**
   - Optional session filtering (`EnableTimeFilter`) mirrors the MT5 hour/minute logic, including overnight sessions when the start hour is greater than the end hour.
   - Whenever trading is outside the permitted window all open positions are liquidated immediately, matching the original expert.
   - Stop-loss and take-profit distances expressed in points are converted into price units using the security price step and passed to `StartProtection`.

## Parameters
- `OrderVolume` – volume per order (used for both buy and sell entries).
- `EnableTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute` – session window settings.
- `StopLossPoints`, `TakeProfitPoints` – protective distances in points (0 disables the respective leg).
- `BuyOpenEnabled`, `SellOpenEnabled`, `BuyCloseEnabled`, `SellCloseEnabled` – enable or disable long/short entries and exits individually.
- `SignalCandleType` – timeframe used for the custom indicator and trading signals (default 4-hour candles).
- `JmaLength`, `JmaPhase` – Jurik smoothing settings (phase honoured when the underlying indicator exposes it).
- `AppliedPriceMode` – applied price enumeration identical to the MT5 version (close, open, median, typical, TrendFollow variants, Demark, etc.).
- `DigitRounding` – rounding multiplier that mimics the `Digit` input of the MQL indicator.
- `SignalBar` – how many closed bars to look back when evaluating color transitions (default 1).

## Notes
- The strategy uses `SubscribeCandles` and high-level order helpers (`BuyMarket`, `SellMarket`) as recommended by the StockSharp conversion guidelines.
- Jurik phase is applied via reflection; if the runtime implementation does not expose a `Phase` property the default behaviour is used automatically.
- Rounding requires a valid `Security.PriceStep`. When unavailable, indicator values remain unrounded.
- No Python version is provided, as requested.

## Usage
1. Attach the strategy to a security and connection capable of providing the configured `SignalCandleType`.
2. Configure the applied price, Jurik parameters, session times and money-management inputs as desired.
3. Start the strategy; it will manage a single position, respecting stop-loss/take-profit protections and the color-driven signals described above.
