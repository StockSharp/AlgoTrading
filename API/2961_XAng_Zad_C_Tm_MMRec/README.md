# XAng Zad C TM MM Rec Strategy

## Overview
This strategy is a C# port of the MetaTrader expert advisor **Exp_XAng_Zad_C_Tm_MMRec**. It trades adaptive price envelopes calculated by the custom *XAng Zad C* indicator and adds a time-based trading window together with a simple money-management recounter. The goal is to capture breakouts when the adaptive upper and lower lines cross each other while dynamically scaling position size after a configurable number of losing trades.

### Core logic
- **Indicator** – the XAng Zad C indicator produces an upper and lower adaptive channel. The C# version reproduces the envelope calculation and supports several moving-average smoothers (SMA, EMA, SMMA, LWMA). Exotic smoothers from the original script fall back to EMA.
- **Entry signals** – when the previous candle shows the upper line above the lower line and the current bar closes with the upper line falling below the lower line, a bullish breakout is detected. The opposite configuration produces a bearish breakout. The `SignalShift` parameter defines how many closed candles back should be compared.
- **Exit signals** – optional flags allow closing longs when the upper line moves back under the lower line and closing shorts on the inverse event. Positions are also closed immediately when the configured trading window ends.
- **Money management** – the strategy keeps a list of historical trade results. If the most recent `BuyLossTrigger` (or `SellLossTrigger`) losing trades appear within the last `BuyTotalTrigger` (or `SellTotalTrigger`) trades, the next position uses the reduced volume. Otherwise the normal volume is restored.
- **Risk control** – static stop loss and take profit targets are applied in multiples of the instrument price step. If either level is reached during the candle, the position is flattened at the corresponding price.

## Parameters
| Name | Description |
| --- | --- |
| `NormalVolume` | Default order size used when there is no recent losing streak. |
| `ReducedVolume` | Order size applied after a sequence of losing trades. |
| `BuyTotalTrigger` / `SellTotalTrigger` | Number of historical trades inspected when evaluating the loss counter. |
| `BuyLossTrigger` / `SellLossTrigger` | Required losing trades (within the above window) to switch to the reduced volume. |
| `EnableBuyEntries` / `EnableSellEntries` | Allow long or short entries. |
| `EnableBuyExit` / `EnableSellExit` | Allow automatic exit signals based on channel crossings. |
| `UseTradingWindow` | Enable the time filter. Outside the window all positions are closed and no new orders are submitted. |
| `WindowStart` / `WindowEnd` | Start and end times of the daily trading window (UTC). The window can span midnight. |
| `StopLoss` | Stop loss distance expressed in multiples of `Security.PriceStep`. Set to `0` to disable. |
| `TakeProfit` | Profit target distance expressed in multiples of `Security.PriceStep`. Set to `0` to disable. |
| `SignalShift` | Number of already closed candles used for the crossover comparison. |
| `CandleType` | Candle data type used for the indicator (default: 4-hour candles). |
| `SmoothMethods` | Moving-average smoother inside the indicator. Unsupported values automatically use EMA. |
| `MaLength` | Smoothing length for the indicator. |
| `MaPhase` | Additional phase parameter retained from the original indicator (currently informational). |
| `Ki` | Ratio controlling how quickly the adaptive envelopes react to price changes. |
| `AppliedPrices` | Price source used to feed the indicator (close, open, median, etc.). |

## Notes compared to the MQL5 version
- MetaTrader money-management helpers relied on global trade history. The C# version tracks completed trades locally and applies the same trigger logic.
- Lot sizing is expressed directly as strategy volume. Adjust `NormalVolume`/`ReducedVolume` to match the target quantity for your venue.
- Time windows are configured with `TimeSpan` values. When `WindowStart` equals `WindowEnd`, trading is disabled (matching the zero-width window behaviour of the original script).
- The strategy assumes full position reversals and does not keep partial positions from previous signals.
- Unsupported smoothing types (JJMA, JurX, ParMA, T3, VIDYA, AMA) default to EMA. Consider extending `CreateMovingAverage` if you require a specific alternative.

## Usage tips
1. Choose a candle type that matches the indicator timeframe used in MetaTrader (default: H4).
2. Tune stop-loss and take-profit distances based on the instrument tick size to approximate the point-based values from the original EA.
3. Optimise the money-management triggers to reflect the asset volatility and your risk tolerance.
4. Monitor indicator behaviour on a chart (upper/lower channel lines) to confirm that the reconstructed indicator matches expectations before live trading.
