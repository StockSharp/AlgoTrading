# VininI Trend LRMA Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tracks the endpoint `L` of a rolling linear regression of finished candle closes. Defaults: `CandleType = 4 hours`, `Period = 13`, `UpLevel = 10`, `DnLevel = -10`, `Mode = Breakdown`. Trading waits for formed indicators and trading permission.

In **Breakdown**, thresholds apply to the relative oscillator `O = 100 × (Close − L) / L`, in percent. Buy on `previousO <= UpLevel && O > UpLevel`; sell on `previousO >= DnLevel && O < DnLevel`. These are not absolute price levels of LRMA.

In **Twist**, buy when the regression endpoint slope turns positive after a negative direction; sell when it turns negative after a positive direction. A flat segment preserves the preceding nonzero direction.

A buy is allowed with `Position <= 0`, a sell with `Position >= 0`. Close an opposite filled position first, then open `Volume` in the new direction with market orders. A same-direction signal does not add to an existing position. No stop-loss or take-profit.
