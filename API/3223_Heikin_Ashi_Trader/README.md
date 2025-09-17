# Heikin Ashi Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader 4 expert "Heikin Ashi Trader" into StockSharp. It keeps the multi-indicator confirmation logic of the original robot and implements it with the high-level candle subscription API so every decision is based on finished bars only.

## Details
- **Indicators**:
  - Heikin-Ashi candles calculated from the working timeframe.
  - Two linear weighted moving averages (LWMA) using the typical candle price (`(high + low + close) / 3`).
  - A stochastic oscillator (`%K/%D/Smooth` periods are user-configurable).
  - Momentum (distance from the neutral 100 level).
  - Moving Average Convergence Divergence (MACD).
- **Entry Criteria**:
  - **Long**: The latest Heikin-Ashi candle must be bullish, at least one of the last three stochastic values must be above the overbought level, the fast LWMA has to be above the slow LWMA, the momentum distance from 100 must exceed the buy threshold, and the MACD line must be above its signal.
  - **Short**: Mirror conditions – bearish Heikin-Ashi candle, stochastic under the oversold level, fast LWMA below slow LWMA, momentum distance above the sell threshold, and MACD line under its signal.
  - Optionally flatten the opposite exposure before taking the new trade (`CloseOppositePositions`).
- **Position Management**:
  - Fixed stop-loss and take-profit in pips (derived from the security price step).
  - Optional trailing stop that follows the close once the trade advances by `TrailingStopPips`.
  - Break-even logic that moves the stop to `Entry ± BreakEvenOffsetPips` after price travels `BreakEvenTriggerPips` in favour of the position.
  - Manual kill switch (`ForceExit`) to flatten everything on the next candle.
- **Differences vs. the MT4 version**:
  - The original EA evaluated momentum on a higher timeframe. This port keeps the same indicator periods but reads them from the primary candle stream to remain within the StockSharp high-level API. Parameters allow the thresholds to be tuned if you want to recreate the original sensitivity.
  - Money-based stop rules from the MT4 code are not included. Risk is handled through price-based stops and the break-even module.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe (or any other candle type) used for all indicators and trading decisions. |
| `FastMaPeriod`, `SlowMaPeriod` | Periods of the fast and slow linear weighted moving averages (typical price). |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K/%D` lengths and smoothing factor of the stochastic oscillator. |
| `StochasticOverbought`, `StochasticOversold` | Stochastic thresholds that must be crossed during the last three finished values. |
| `MomentumPeriod` | Momentum indicator length. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimum absolute distance from the 100 line required for long/short trades. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD configuration. |
| `CloseOppositePositions` | Close the opposite side before entering a new trade. |
| `MaxPositions` | Maximum net exposure per direction (`0` = unlimited). |
| `TradeVolume` | Volume of each new order; also assigned to the strategy `Volume`. |
| `UseStopLoss`, `StopLossPips` | Enable and size the protective stop in pips. |
| `UseTakeProfit`, `TakeProfitPips` | Enable and size the take-profit in pips. |
| `UseTrailingStop`, `TrailingStopPips` | Enable trailing stop logic and define its distance. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-even activation distance and the locked-in offset. |
| `ForceExit` | When `true`, all positions are closed on the next processed candle. |

## Implementation Notes
- The strategy subscribes to candles through `SubscribeCandles().BindEx(...)` so indicators receive finished values and the code never calls `GetValue()` directly.
- Pip conversion uses the instrument `PriceStep`; if your market quotes fractional pips, configure the security step appropriately.
- Trailing and break-even updates only move the stop in the favourable direction. Reset logic clears cached stop/target values whenever a trade is closed so new positions start with fresh risk settings.
