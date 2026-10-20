# Williams Alligator ATR

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

All three Alligator lines use smoothed median prices (high+low)/2. Their Length and Shift parameters control the smoothing and completed-bar delay. Teeth is computed and must be formed, but it is not an extra entry filter. After every line and Wilder ATR are formed, Lips−Jaw crossing from <=0 to >0 opens one Volume long from flat. Crossing from >=0 to <0 closes the entire actual long; never open short. At entry freeze ATR×AtrMultiplier as native local market stop distance from the actual fill. Bid/ask quotes can execute it between candles. No cooldown, take profit or moving ATR stop.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
