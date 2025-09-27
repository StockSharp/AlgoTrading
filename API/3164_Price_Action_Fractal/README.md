# Price Action Fractal Strategy

This strategy is a C# port of the MetaTrader "PRICE_ACTION" expert advisor. It combines Williams fractals with weighted moving averages, momentum and MACD filters to trade breakouts confirmed by price action on the selected timeframe.

## Idea

1. Analyse completed candles only; every decision is made on the bar close of the configured timeframe.
2. Detect new bullish or bearish fractals using a 5-candle window. A fresh down fractal signals potential support, while a fresh up fractal signals potential resistance.
3. Confirm the directional bias with two linear weighted moving averages (LWMA). Long trades require the fast LWMA to be above the slow LWMA, short trades require the opposite.
4. Validate momentum by checking the absolute deviation of the Momentum indicator from the neutral 100 level on the higher timeframe.
5. Use a MACD (12,26,9 by default) filter: bullish setups demand MACD to be above its signal line, bearish setups demand MACD below the signal line.
6. Once all filters agree, enter in the direction of the breakout and manage the position with fixed stops, a trailing stop and an optional break-even shift.

## Entry rules

- **Long entry**
  - A new down fractal forms on the current candle (five-bar pattern).
  - Fast LWMA &gt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - MACD main line &gt; MACD signal line.
  - Position size is based on the strategy volume and limited by `MaxPositionUnits`.

- **Short entry**
  - A new up fractal forms on the current candle.
  - Fast LWMA &lt; Slow LWMA.
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`.
  - MACD main line &lt; MACD signal line.

## Exit rules

- Fixed stop-loss (`StopLossPoints`) and fixed take-profit (`TakeProfitPoints`) expressed in price steps.
- Optional trailing stop (`TrailingStopPoints`) that follows the most favourable price once the position gains at least the trailing distance.
- Optional break-even protection: after reaching `BreakEvenTriggerPoints` the stop is shifted to `EntryPrice ± BreakEvenOffsetPoints`.
- Exits are performed with market orders; all calculations rely on candle highs/lows to detect stop hits.

## Position management

- The strategy maintains a single aggregated position per symbol.
- `Volume` defines the base order size. When reversing, the strategy closes the opposite exposure first and then opens a new position with the requested size.
- `MaxPositionUnits` caps the absolute position value to avoid over-sizing.

## Parameters

- `CandleType` – timeframe used for every indicator and decision (equivalent to the MQL variable `T`).
- `FastMaPeriod` / `SlowMaPeriod` – lengths of the weighted moving averages (`FastMA`, `SlowMA`).
- `MomentumPeriod` – momentum lookback length (fixed at 14 in the MQL script).
- `MomentumThreshold` – minimal absolute deviation from 100 required to confirm momentum (`Mom_Buy`/`Mom_Sell`).
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD configuration (12/26/9 by default).
- `StopLossPoints`, `TakeProfitPoints` – price-step distances for protective orders (`Stop_Loss`, `Take_Profit`).
- `TrailingStopPoints` – trailing stop distance (`TrailingStop`).
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – break-even trigger and offset (`WHENTOMOVETOBE`, `PIPSTOMOVESL`).
- `FractalLifetime` – number of candles a detected fractal remains valid (`CandlesToRetrace`).
- `MaxPositionUnits` – maximum absolute position size (`Max_Trades` constraint in lot units).
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – switches for the respective exit mechanisms.

## Differences from the original EA

- Portfolio-wide features such as money-based take-profit, equity stop, and email/notification alerts are not implemented.
- Lot optimisation routines from MetaTrader are simplified; the strategy uses StockSharp volume normalization.
- Protective orders are executed with market exits rather than pending order modifications because StockSharp handles risk management differently.

## Files

- `CS/PriceActionFractalStrategy.cs` – strategy implementation in C#.
- Python version is not provided yet.
