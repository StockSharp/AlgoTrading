# PowerZone Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy identifies "PowerZone" order blocks created by a bearish candle followed by consecutive bullish candles (and vice versa). A breakout above the bull zone triggers a long trade, while a breakdown below the bear zone opens a short. Targets and stops are based on the zone's range.

## Details

- **Entry Criteria**:
  - **Long**: Bearish candle `Periods+1` bars ago followed by `Periods` bullish candles and price breaking above the zone high.
  - **Short**: Bullish candle `Periods+1` bars ago followed by `Periods` bearish candles and price breaking below the zone low.
- **Long/Short**: Both sides.
- **Exit Criteria**: Take profit and stop loss multiples of the zone range.
- **Indicators**: None.
- **Default Values**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate

## Calculation and execution

C# and Python use finished candles. At decision bar `t`, the anchor is `t - Periods - 1`; the next `Periods` bars through `t - 1` must all close strictly above their opens for a bull zone, or strictly below for a bear zone. The anchor has the opposite strict color. Dojis do not qualify. The current decision candle is excluded from confirmation, so zones are never known before their confirmation completes.

Require `100 * abs(Close[t - 1] - anchor Close) / abs(anchor Close) >= Threshold`; a zero anchor close is invalid. The zone is the anchor's body `[min(Open, Close), max(Open, Close)]`, or its `[Low, High]` when `UseWicks=true`. Confirmation candle extremes do not enlarge the zone. Its width must be positive. Keep the latest bull and bear zones independently until replaced, with at most one accepted entry per zone.

Long requires current `Close > bull zone high`; short requires `Close < bear zone low`. Equality holds. A newly confirmed zone can enter on its first decision candle even if price is already beyond its boundary. Enter `Volume` from flat or fully reverse with `Volume + abs(Position)`; do not add to the same side. If both directions qualify from flat, long takes priority. Pending orders block further orders. No EMA, cooldown or other indicator is used.

At accepted entry, freeze `zone width * StopLossFactor` and `zone width * TakeProfitFactor`. Place these distances around the actual fill basis, volume-weighted for same-side partial additions; reductions retain the basis and reversals reset it. Later zones do not move the held position's distances. Local protection checks executable bid for long, ask for short and finished candle closes, then submits a market order for the full remaining position. It takes priority over a new entry on the same candle. Candle high/low touches are not assumed intrabar executions; gaps can change the fill price.

The defaults above are shared: API parameters `TakeProfitFactor=1.5`, `StopLossFactor=1`, and five-minute `CandleType`. `UseWicks` is the published parameter name; the C# property is `IsUseWicks`. `Periods` and factors must be positive; `Threshold` is a nonnegative percent. These definitions make the brief's previously unspecified threshold, boundary, consumption and fill-anchor rules explicit.
