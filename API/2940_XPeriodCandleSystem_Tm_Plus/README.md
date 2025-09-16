# XPeriod Candle System TM Plus Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor `Exp_XPeriodCandleSystem_Tm_Plus`. The original robot relies on the custom *XPeriod Candle System* indicator that smooths candle data and colors bars according to Bollinger Band breakouts. The translated version reproduces this behaviour by applying exponential smoothing to the OHLC series, mapping the same applied price modes, and driving trades from the resulting color states. A time-based exit and configurable protective orders complement the breakout logic.

## Trading Logic

1. **Smoothed candles** – Exponential moving averages with configurable length build synthetic open, high, low, and close values that approximate the source indicator.
2. **Applied price** – The user can select any of the twelve price formulas (close, open, median, trend-following variations, Demark, etc.) before feeding data into the Bollinger Bands.
3. **Band analysis** – A Bollinger Bands indicator (length and deviation configurable) processes the smoothed price series. Finished bands are required before signals are evaluated.
4. **Color states** –
   - Bullish bar above the upper band → color `0` (breakout).
   - Bearish bar below the lower band → color `4` (breakdown).
   - Other bullish bars → color `1`; other bearish bars → color `3`.
   - A configurable breakout offset (converted to price units using the symbol tick size when possible) avoids false triggers.
5. **Entries** – The strategy looks at the candle defined by `SignalBar` and its predecessor:
   - Open long when the previous bar was a bullish breakout (`0`) and the signal bar is not.
   - Open short when the previous bar was a bearish breakout (`4`) and the signal bar is not.
6. **Exits** –
   - Close longs when the reference bar is bearish (`> 2`).
   - Close shorts when the reference bar is bullish (`< 2`).
   - Optional holding timer (`TimeTrade` and `HoldingMinutes`) closes positions after the specified minutes.
7. **Risk** – `StartProtection` deploys optional absolute take-profit and stop-loss distances for every trade.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Base order size used for market entries. | 0.1 |
| `BuyPosOpen` / `SellPosOpen` | Enable/disable long or short entries. | `true` |
| `BuyPosClose` / `SellPosClose` | Allow long or short position exits. | `true` |
| `TimeTrade` | Enables the time-based exit filter. | `true` |
| `HoldingMinutes` | Maximum holding time before the time filter closes a position. | 960 |
| `CandleType` | Candle data type (time frame) requested from the market. | 4 hours |
| `Period` | Length of the smoothing exponential moving averages. | 5 |
| `BollingerLength` | Number of smoothed bars inside the Bollinger calculation window. | 20 |
| `BandsDeviation` | Band width multiplier. | 1.001 |
| `AppliedPriceMode` | Price transformation used before the Bollinger indicator (close, open, median, trend-following, Demark, etc.). | Close |
| `SignalBar` | Index of the bar used for signal evaluation (1 = last closed bar). | 1 |
| `StopLoss` / `TakeProfit` | Absolute distances (in price units) used by the protective engine. | 1000 / 2000 |
| `Deviation` | Extra breakout offset added above/below the Bollinger bands. | 10 |

## Usage Notes

- The smoothing step uses exponential moving averages to replicate the proprietary XPeriod calculation. Smaller periods keep the synthetic candles closer to market prices, while larger periods emphasise trend structure.
- `SignalBar` must remain within the stored history (up to 14 positions after the current bar). Values greater than the available history will automatically skip trading.
- The breakout offset is multiplied by `PriceStep` when the security exposes a tick size. This keeps the behaviour similar to the MetaTrader version where `Deviation` is defined in points.
- `StopLoss` and `TakeProfit` are specified in absolute price units. Set them to zero to disable protective orders while keeping the management infrastructure active.
- No Python translation is provided yet; this folder contains only the C# implementation.
