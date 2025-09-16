# ScalpWiz 9001 Strategy

## Overview
ScalpWiz 9001 is a layered breakout scalping system that replicates the behaviour of the MetaTrader expert advisor with the same name. The strategy measures how far the latest candle closes beyond the Bollinger Bands envelope and, when volatility expands sharply, it deploys a grid of pending stop orders above or below the market. The original money-management module is preserved: each pending order can either use a fixed lot or risk a configurable percentage of account equity.

Once one of the stop orders is filled, the remaining orders are cancelled, while the active position is protected with a traditional stop loss, take-profit and a trailing component that only starts trailing after an additional buffer is achieved. The strategy is intended for high-frequency scalping on lower timeframes, but it can be executed on any instrument supported by StockSharp.

## Signal Logic
1. Subscribe to the configured timeframe and compute 20-period Bollinger Bands with deviation factor `BandsDeviation` (default 2).
2. Check how far the closing price is from the upper or lower band. When the close exceeds the band by at least the fourth level distance (`Level3Pips` converted to price), the strategy prepares to fade the move:
   - Close above upper band → place sell-stop orders below the market.
   - Close below lower band → place buy-stop orders above the market.
3. Four pending orders are placed at increasing distances (`Level0Pips` … `Level3Pips`). Each order uses either the fixed volume or the risk percentage assigned to that tier. Orders expire after `ExpirationMinutes` if left untouched.
4. When an entry order trades, all outstanding orders are cancelled. The filled position is managed by the stop loss (`StopLossPips`), take profit (`TakeProfitPips`) and trailing parameters (`TrailingStopPips`, `TrailingStepPips`). Trailing only moves the protective stop when price travels at least `TrailingStopPips + TrailingStepPips` away from the entry.
5. Exits are executed with market orders once the trailing stop or the profit target is touched on a completed candle.

## Parameters
- **Candle Type** – timeframe for Bollinger calculations.
- **Bands Period / Bands Deviation** – Bollinger configuration.
- **Stop Loss (pips)** – protective stop distance in pips.
- **Take Profit (pips)** – profit target distance in pips.
- **Trailing Stop (pips)** – trailing stop distance that follows the move after the extra buffer.
- **Trailing Step (pips)** – additional distance required before trailing activates.
- **Expiration (minutes)** – lifetime of pending stop orders. Set to 0 to keep orders indefinitely.
- **Management Mode** – choose between `FixedVolume` and `RiskPercent`.
- **Level 0-3 Value** – fixed lot or risk percent for each pending layer.
- **Level 0-3 Pips** – entry offsets for each pending layer.

## Money Management
When `ManagementMode` equals `RiskPercent`, the strategy computes the order volume from the account equity and the configured stop loss distance:

```
order volume = (equity × riskPercent / 100) / (stopOffset / priceStep × stepPrice)
```

If market metadata (price step, step price or volume step) is unavailable, the order size falls back to zero for safety. With `FixedVolume`, the layer values are used directly and rounded to the instrument volume step and bounds.

## Trailing and Protection
- Stop loss and take profit are initialised using pip distances relative to the actual fill price.
- Trailing logic mirrors the MetaTrader implementation: the stop is not moved until price advances by `TrailingStop + TrailingStep`, and thereafter it keeps a gap of `TrailingStop`.
- Exits are issued as market orders, ensuring compatibility with venues that do not support server-side protective orders.

## Practical Notes
- Configure the pip distances according to the instrument tick size. For five-digit FX symbols, each pip corresponds to ten price steps and the strategy automatically adjusts for this by inspecting the security decimals.
- Because the strategy relies on stop orders, check broker-specific stop level requirements and adjust level distances if necessary.
- Risk-percent sizing requires valid portfolio valuation and security step metadata; otherwise, the order volume will evaluate to zero.
- The strategy operates on completed candles and therefore reacts once per bar, which smooths noise compared to the original tick-based expert advisor.
