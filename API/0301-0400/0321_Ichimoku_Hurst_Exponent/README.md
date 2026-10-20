# Ichimoku Hurst Exponent
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend strategy combining Tenkan/Kijun crossings, the Ichimoku cloud and a Hurst filter calculated from logarithmic returns. Signals use finished candles.

## Calculation

Tenkan and Kijun are `(highest High + lowest Low) / 2` over their respective windows. The unshifted Senkou A is their average; unshifted Senkou B is the high/low midpoint over `SenkouSpanBPeriod`. The strategy uses the causal displaced spans produced by StockSharp Ichimoku, with `KijunPeriod` displacement and its initialization buffer; no future candles are read.

For the latest `HurstPeriod` closes, calculate `n = HurstPeriod - 1` log returns `r[i] = ln(C[i] / C[i-1])`. Let `mu` be their mean, `X[k] = sum(r[i] - mu)`, `R = max(X) - min(X)` and `S` their population standard deviation. The filter is `H = ln(R / S) / ln(n)`, using StockSharp HurstExponent on these returns. The window starts with the first finished candle. At least eleven closes, a complete window and nonzero variance are required. A nonpositive close restarts the return window; an unavailable Hurst value never enables an entry.

## Trading

- **Long entry:** flat position, close strictly above both cloud spans, Tenkan crossing from `<= Kijun` to `> Kijun`, `H > HurstThreshold`, positive formed ATR and expired cooldown.
- **Short entry:** flat position, close strictly below both spans and the opposite Tenkan/Kijun crossing, with the same Hurst, ATR and cooldown conditions.
- **Signal exit:** close the entire long when the close is below both spans or Tenkan crosses down; close the entire short on the symmetric conditions. These exits remain active during cooldown and do not require the Hurst entry filter.
- **Orders:** market entries of `Volume` and exits of the actual remaining position. An uncompleted order blocks another signal order. An exit closes the position before a later entry.
- **Cooldown:** each signal order restarts `SignalCooldownBars`; the next entry can occur that many finished candles later.

## Protection

Wilder ATR uses `AtrPeriod`. Each entry freezes a stop distance of `StopLossAtr * ATR` from its signal candle, anchored at the actual entry execution price. Local market protection processes Level1 updates and can flatten the position between signal candles. The distance does not trail subsequent ATR values. `StopLossAtr = 0` disables protection for the run. There is no take-profit or trailing stop.

## Defaults

| Parameter | Default |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 100 closes |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 minutes |

Tests compare every signal order and its fills with independently calculated cloud spans and log-return R/S values on the packaged BTC/TON archive. Separate runs verify Hurst warm-up, frozen ATR distances and real intrabar protective fills.

The archive protection comparison uses Tenkan/Kijun/Senkou B periods 15/40/70 and HurstThreshold 0.45 to exercise a stop at the default 2 × ATR distance; the default 9/26/52 signals are checked separately. Protection runs use an entry volume of 3.

