# EXP FIBO ZZ Strategy

## Overview
The EXP FIBO ZZ strategy is a C# port of the MetaTrader 4 expert advisor `EXP_FIBO_ZZ_V1en`. It reproduces the original breakout
logic: monitor the last confirmed ZigZag corridor, place a buy stop above the swing high and a sell stop below the swing low, and
attach Fibonacci-based stop-loss and take-profit orders. The StockSharp version exposes all configurable inputs through
`StrategyParam` objects, adds extensive validation, and keeps the original money-management options including balance-based risk
sizing and the break-even stop adjustment.

## Trading Logic
1. **Data preparation**
   - The strategy subscribes to the configured `CandleType` (default: 1-minute candles) and feeds the series into `Highest` and
     `Lowest` indicators with a length equal to `ZigZagDepth`.
   - A lightweight ZigZag detector tracks the most recent three pivot prices. A new pivot is registered only when:
     * The candle high/low equals the indicator output.
     * At least `ZigZagBackstep` bars passed since the previous turning point.
     * The price deviation from the current pivot exceeds `ZigZagDeviationPips` (expressed in MetaTrader pips).

2. **Corridor validation**
   - Once three pivots are available, the two oldest define the corridor. Trading continues only if the corridor height is between
     `MinCorridorPips` and `MaxCorridorPips` and the latest pivot sits strictly inside the band with a small broker-style buffer.
   - Outside the user-specified trading window (`StartHour/StartMinute` to `StopHour/StopMinute`) all pending orders are cancelled.

3. **Order placement**
   - Buy and sell stop prices are calculated as the corridor boundaries plus/minus `EntryOffsetPips`.
   - Stop-loss distance equals `corridor * FiboStopLoss / 100`. Take-profit distance follows the MetaTrader formula
     `corridor * (FiboTakeProfit / 100 - 1)` with negative values clamped to zero.
   - Before placing orders the strategy computes the trade volume. If `RiskPercent > 0`, the code multiplies the selected capital
     source (equity when `UseBalanceForRisk` is `true`, otherwise equity minus blocked margin) by the risk percentage and divides
     the result by the reference price. The volume is snapped to the exchange lot grid and clipped to the exchange limits. When
     the required information is unavailable the algorithm falls back to `FixedVolume`.
   - Active entry orders are modified whenever the target price or volume changes; otherwise new orders are submitted.

4. **Position management**
   - As soon as a position opens the algorithm cancels the opposite pending order and registers protective orders:
     * Stop-loss via `SellStop`/`BuyStop` at the pre-computed distance.
     * Optional take-profit via `SellLimit`/`BuyLimit`.
   - The optional break-even module (`EnableBreakEven`) mirrors the original `MovingInWL` routine. After accumulating
     `BreakEvenTriggerPips` of profit the stop is moved to the entry price plus/minus `BreakEvenOffsetPips`, guaranteeing at least
     a tiny gain while preventing repeated adjustments.

5. **Session maintenance**
   - Leaving the trading window or flattening the position cancels any outstanding pending or protection orders. The method
     `OnStopped` also clears every order when the strategy terminates.

## Parameters
| Name | Description | Default | Notes |
| --- | --- | --- | --- |
| `CandleType` | Data series used to build the ZigZag pivots. | `1m TimeFrame()` | Supports any StockSharp candle type. |
| `ZigZagDepth` | Minimum number of candles between ZigZag swings. | `12` | Matches the MT4 `ExtDepth` input. |
| `ZigZagDeviationPips` | Minimum deviation (in MetaTrader pips) before accepting a new pivot. | `5` | Mirrors `ExtDeviation`. |
| `ZigZagBackstep` | Minimum bar count before the ZigZag can reverse again. | `3` | Equivalent to `ExtBackstep`. |
| `EntryOffsetPips` | Distance in pips added above/below the corridor when placing stop orders. | `5` | Mirrors `n_pips`. |
| `MinCorridorPips` | Lower bound for the corridor size. | `20` | Mirrors `Min_Corridor`. |
| `MaxCorridorPips` | Upper bound for the corridor size. | `100` | Mirrors `Max_Corridor`. |
| `FiboStopLoss` | Fibonacci ratio applied to the corridor to derive the stop-loss distance. | `61.8` | Mirrors `Fibo_StopLoss`. |
| `FiboTakeProfit` | Fibonacci ratio applied to compute the take-profit target. | `161.8` | Mirrors `Fibo_TakeProfit`. |
| `StartHour` / `StartMinute` | Beginning of the allowed trading session. | `00:01` | Orders are cancelled outside the window. |
| `StopHour` / `StopMinute` | End of the trading session. | `23:59` | Supports overnight sessions that wrap midnight. |
| `UseBalanceForRisk` | Choose equity (`true`) or available cash (`false`) for risk sizing. | `true` | Mirrors `Choice_method`. |
| `RiskPercent` | Fraction of capital allocated to the next trade. | `1` | Set to `0` to disable risk-based sizing. |
| `FixedVolume` | Lot size used when risk sizing is disabled or unavailable. | `0.1` | Mirrors the `Lots` input. |
| `EnableBreakEven` | Enables the break-even stop adjustment. | `true` | Mirrors `MovingInWL`. |
| `BreakEvenTriggerPips` | Profit in pips required before moving the stop. | `13` | Mirrors `LevelProfit`. |
| `BreakEvenOffsetPips` | Offset in pips applied to the break-even stop. | `2` | Mirrors `LevelWLoss`. |
| `DrawCorridorLevels` | Plot the active corridor on the chart. | `false` | Mirrors the optional line drawing flag. |

## Implementation Notes
- Pip conversion respects MetaTrader conventions by multiplying the `PriceStep` by 10 for three- and five-digit Forex symbols.
- Order prices and volumes are rounded to the nearest valid increment using the exchange metadata (`PriceStep`, `VolumeStep`,
  `MinVolume`, `MaxVolume`).
- Risk sizing falls back gracefully when portfolio data or reference prices are missing, ensuring the strategy still operates with
  the configured fixed lot.
- The break-even routine cancels and re-registers the protective stop only once per trade and never places the stop beyond the
  entry price.
- When `DrawCorridorLevels` is enabled the strategy draws a vertical segment between the high and low pivots of the current
  corridor, allowing quick visual confirmation of the trading range.

## Differences vs. the MetaTrader Version
- Chart objects, sounds, and screen comments from the MT4 script were omitted; StockSharp logging and chart primitives cover the
  same needs.
- Risk sizing uses portfolio equity and last known prices instead of `MarketInfo` margin values, because those details are broker
  specific and unavailable in a platform-agnostic manner.
- Order management uses the high-level StockSharp API (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) instead of manual ticket
  handling. The behaviour remains equivalent while requiring less boilerplate code.
- The ZigZag detector re-implements the depth/deviation/backstep logic with built-in indicators to stay compatible with
  StockSharp's streaming candle model.
