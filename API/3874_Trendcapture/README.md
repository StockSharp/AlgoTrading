# 3874 Trendcapture Strategy

## Overview

The **Trendcapture Strategy** is a high-level StockSharp port of the MetaTrader expert advisor `MQL/7772/Trendcapture.mq4`. The original EA watches the Parabolic SAR trend direction and waits for a weak ADX environment to enter new positions. After each closed trade it decides whether to keep or flip the trade direction depending on the realized profit, and once an open position gains a few points it pulls the stop to break-even.

This port keeps the behaviour intact while relying on StockSharp's order helpers and indicator bindings. All signals are processed on completed candles of a configurable timeframe.

## Trading Logic

1. **Indicator setup**
   - Parabolic SAR (`ParabolicSar`) with configurable acceleration step and cap.
   - Average Directional Index (`AverageDirectionalIndex`) for the main trend strength value.
2. **Entry selection**
   - Only one position can be open at a time.
   - A long entry is allowed when:
     - The desired direction (derived from the last closed trade) points to buying.
     - The current candle closes above the SAR value.
     - ADX main line is below `20`, indicating the ranging regime required by the original code.
   - A short entry mirrors the rules (desired direction points to selling, close price below SAR, ADX below `20`).
3. **Exit management**
   - Upon each fill the strategy submits stop-loss and take-profit orders at `StopLossPoints` and `TakeProfitPoints` distances (converted through the security price step).
   - When the floating profit reaches `GuardPoints` the active stop is re-issued at the entry price to lock in a break-even floor.
   - Closing trades trigger a direction update: profitable trades keep the same bias, losing or flat trades invert it, reproducing the `OrderProfit()` check from the expert.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle data type used for indicator calculations. | 1-hour time frame |
| `SarStep` | Initial acceleration factor of Parabolic SAR. | `0.02` |
| `SarMax` | Maximum acceleration factor for Parabolic SAR. | `0.2` |
| `AdxPeriod` | Smoothing period of ADX. | `14` |
| `TakeProfitPoints` | Take-profit distance expressed in price steps. | `180` |
| `StopLossPoints` | Stop-loss distance expressed in price steps. | `50` |
| `GuardPoints` | Profit threshold (in price steps) required before moving the stop to break-even. | `5` |
| `MaximumRisk` | Volume scaling factor; `0.03` reproduces the original lot sizing. | `0.03` |

## Usage Notes

- Make sure the selected security exposes `PriceStep` (or at least `MinStep`) so that point distances are converted to price values correctly.
- The base `Volume` property represents the lot size used when `MaximumRisk` equals `0.03`. Increasing the risk factor scales the submitted volume proportionally.
- Because the EA trades at market and immediately places protective orders, there are no pending entries left on the book when the strategy is idle.
- The break-even guard cancels and re-issues the protective stop at the entry price; this mirrors the original `OrderModify` call that moved the stop-loss to breakeven.

## Files

- `CS/TrendcaptureStrategy.cs` – high-level StockSharp implementation of the Trendcapture EA.
- `README_cn.md` – Chinese translation of this document.
- `README_ru.md` – Russian translation of this document.
