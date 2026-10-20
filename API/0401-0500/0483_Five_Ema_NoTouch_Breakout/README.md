# Five Ema NoTouch Breakout

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Process only finished candles after EMA formation. A candle wholly below EMA arms a long breakout above its high; a candle wholly above EMA arms a short breakout below its low. Test the previous setup before replacing it; touching the boundary is not a breakout. Enter only from flat, with Volume units, and clear setup while holding. Freeze stop at the setup's opposite extreme. On the first actual entry fill freeze risk = distance to that stop and take-profit distance = RewardRisk×risk. Native local market protection observes bid/ask quotes between candles. If the fill has already crossed the stop, immediately close the actual position. No cooldown or pyramiding.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
