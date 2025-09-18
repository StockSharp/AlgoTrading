# Surfing 3.0 Strategy

## Overview

This C# strategy is a faithful port of the MetaTrader 4 expert **Surfing 3.0**. It recreates the breakout logic that watches an exponential moving average (EMA) envelope built from candle highs and lows. Whenever the previous bar closes inside the band and the latest closed bar pierces it, the system reacts with a directional trade. The translation relies on StockSharp's high level API, candle subscriptions and built-in indicators instead of hand-written buffers.

The algorithm works exclusively with finished candles from a configurable aggregation. It keeps only the minimal amount of state required to emulate the `iMA` and `iClose` lookbacks used by the original code. Every decision is made once per closed bar, matching the "closed bar" evaluation style of the MQL implementation.

## Indicators

- **High EMA / Low EMA** – Two exponential moving averages calculated on candle highs and lows. They form a dynamic envelope that defines breakout levels for long and short entries.
- **Relative Strength Index (RSI)** – Acts as a trend filter. Long positions require the RSI to be above `LongRsiThreshold`, while shorts are allowed only when it is below `ShortRsiThreshold`.

## Trading Logic

1. Subscribe to candles of type `CandleType` and update the EMA and RSI indicators for every finished bar.
2. Store the previous closed bar values of the close price and the EMA highs/lows. These represent `PriceClose_2`, `PriceHigh_2` and `PriceLow_2` from the original expert.
3. When the latest closed bar (`PriceClose_1`) crosses **above** the high EMA while the previous close was below or equal to it and the RSI filter confirms:
   - Close any open short position.
   - Open a long market order with volume `OrderVolume`.
   - Calculate stop loss and take profit offsets in instrument points.
4. When the latest closed bar crosses **below** the low EMA while the previous close was above or equal to it and the RSI is below the short threshold:
   - Close any open long position.
   - Open a short market order with volume `OrderVolume`.
   - Apply the protective levels using the same point-based distances.
5. Only one net position can be active. Reversal signals always flatten the existing exposure before entering in the opposite direction.
6. Outside the trading window `[TradeStartHour, TradeEndHour)`, no new trades are initiated. Once the clock reaches `TradeEndHour`, the strategy closes any remaining position and resets its internal history, mimicking the `closeAllPos()` call in the MQL version.

## Risk Management

- **Stop Loss / Take Profit** – Expressed in instrument points and converted using the security price step. Both are optional; setting a distance of `0` disables the respective level.
- **Session Flat** – At the end of the allowed trading window every open position is closed at market and the stop/take profit tracking is cleared. This prevents positions from drifting overnight, exactly as the original expert enforced with `startHour` / `endHour`.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `OrderVolume` | Trade volume used for every market order. | `1` |
| `TakeProfitPoints` | Take profit distance expressed in instrument points. | `80` |
| `StopLossPoints` | Stop loss distance expressed in instrument points. | `50` |
| `MaPeriod` | Length of the EMA applied to highs and lows. | `50` |
| `RsiPeriod` | Period of the RSI filter. | `10` |
| `LongRsiThreshold` | Minimum RSI value required to allow long entries. | `40` |
| `ShortRsiThreshold` | Maximum RSI value allowed to enter short positions. | `65` |
| `TradeStartHour` | Hour (exchange time) from which new trades are permitted. | `8` |
| `TradeEndHour` | Hour (exclusive) after which positions are closed and no new trades start. | `18` |
| `CandleType` | Candle aggregation used for all calculations (default: 15-minute candles). | `15m` |

## Notes

- Signals are evaluated strictly on finished candles; intrabar fluctuations are ignored just like in MetaTrader.
- The strategy resets its EMA history when the trading session ends to avoid mixing data from different days.
- Python translation is intentionally omitted in accordance with the project guidelines.
