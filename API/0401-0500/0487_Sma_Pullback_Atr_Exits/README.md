# Sma Pullback Atr Exits

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

On finished candles with both close-price SMAs and Wilder ATR formed, buy only when close < fast SMA and fast SMA > slow SMA; sell short only when close > fast SMA and fast SMA < slow SMA. Equalities give no entry. Enter Volume units only from flat and await pending orders. At the signal freeze SL = ATR×AtrMultiplierSl and TP = ATR×AtrMultiplierTp; native local market protection anchors both distances to the actual fill and follows bid/ask quotes. Later ATR changes cannot move either level. Exit the full remaining position through protection; an opposing signal does not reverse an open trade.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
