# EMA RSI Volatility Adaptive Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a direct port of the MetaTrader expert advisor **EA_MARSI_1-02**. It trades crossovers between two copies of
Integer's custom *EMA_RSI_VA* indicator, a volatility-adaptive moving average driven by the Relative Strength Index (RSI).
Whenever the slow line crosses the fast line the engine reverses the net position, reproducing the original "flip on crossover"
behaviour while respecting StockSharp's order handling best practices.

## Indicator mechanics

The original MQL package ships with a custom indicator called `EMA_RSI_VA`. It calculates a price-smoothed EMA whose effective
length is modulated by the distance of RSI from its neutral value. The StockSharp port introduces the
`EmaRsiVolatilityAdaptiveIndicator` class that replicates the formula precisely:

1. Compute RSI on the selected `AppliedPrice` source with period `RSIPeriod`.
2. Measure the RSI distance from 50 (`|RSI - 50| + 1`), which acts as a volatility proxy.
3. Derive an adaptive multiplier
   `multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`.
4. Multiply the configured EMA period by this multiplier to obtain a dynamic length `pdsx`.
5. Apply the standard EMA recursion with smoothing factor `2 / (pdsx + 1)` using the candle's applied price as input.

Large RSI excursions shorten the smoothing window and make the line react faster; a flat RSI lengthens the window and damps
noise. Both the slow and fast lines expose the full set of price modes supported by `StockSharp.Messages.AppliedPrice`.

## Trading rules

- **Signal detection**
  - *Sell / short*: previous slow < previous fast **and** current slow ≥ current fast.
  - *Buy / long*: previous slow > previous fast **and** current slow ≤ current fast.
- **Execution**
  - The strategy only analyses finished candles from the configured candle series.
  - When a signal occurs it submits a market order sized to both close the existing exposure and open the new direction.
  - Exchange limits are respected through `Security.MinVolume`, `Security.VolumeStep`, and `Security.MaxVolume`.
- **Reversals**
  - Orders are netted so that a single `SellMarket` or `BuyMarket` call takes the position across the zero line, matching the
    MQL behaviour where an opposite signal immediately flips the trade.

## Risk management

- `TakeProfitPoints` and `StopLossPoints` replicate the expert advisor's TP/SL fields (expressed in price points). When either
  value is non-zero the strategy starts StockSharp's protection manager with absolute price offsets and `useMarketOrders = true`
  to mirror the original `OrderSend` stop/limit modification loop.
- `UseBalanceMultiplier` implements the `use_Multpl` toggle. When active the effective order volume becomes
  `Volume * PortfolioEquity / MaxDrawdown` with a defensive clamp to exchange constraints.
- The base class `StartProtection()` call is still executed so that external risk modules can attach trailing or break-even
  logic if required.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Volume` | `0.1` | Base market order size before any balance multiplier is applied. |
| `TakeProfitPoints` | `0` | Take-profit distance in instrument points; `0` disables the take-profit leg. |
| `StopLossPoints` | `0` | Stop-loss distance in instrument points; `0` disables the protective stop. |
| `UseBalanceMultiplier` | `false` | Enables balance-proportional position sizing identical to `use_Multpl` in the EA. |
| `MaxDrawdown` | `10000` | Denominator for the balance multiplier; corresponds to the EA's `Max_drawdown`. |
| `SlowRsiPeriod` | `310` | RSI lookback for the slow EMA_RSI_VA line. |
| `SlowEmaPeriod` | `40` | Base EMA length for the slow line before RSI adaptation. |
| `SlowAppliedPrice` | `Close` | Price mode forwarded to the slow indicator. |
| `FastRsiPeriod` | `200` | RSI lookback for the fast EMA_RSI_VA line. |
| `FastEmaPeriod` | `50` | Base EMA length for the fast line before RSI adaptation. |
| `FastAppliedPrice` | `Close` | Price mode forwarded to the fast indicator. |
| `CandleType` | `TimeFrame(1m)` | Candle series used for calculations. |

## Implementation notes

- The port is written with StockSharp's high-level API (`SubscribeCandles().Bind(...)`) to avoid manual indicator loops.
- Only completed candles are processed, matching `CopyBuffer(..., 1, 2, ...)` calls in the MQL source.
- Volume normalisation uses `Security.MinVolume`, `Security.VolumeStep`, and `Security.MaxVolume`, preventing invalid orders on
  real exchanges.
- A Python version is intentionally omitted as requested; the directory only contains the C# implementation and documentation.

The resulting behaviour mirrors the source EA while exposing StockSharp-friendly parameters and risk controls suitable for
Designer, Runner, or any custom host built on the StockSharp API.
