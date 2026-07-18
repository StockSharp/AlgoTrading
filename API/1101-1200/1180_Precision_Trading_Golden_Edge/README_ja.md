# 精密トレーディング戦略：Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このゴールド向けスキャルピング戦略は、高速EMAと低速EMAのクロスオーバーをHull Moving Averageの方向と一致させます。RSIがモメンタムを確認し、ボラティリティが十分な場合にのみ取引が発生します。

## 詳細

- **エントリー条件**:
  - **ロング**: 高速EMAが低速EMAを上抜け、RSI > 55、HMA上昇中、ボラティリティフィルター通過。
  - **ショート**: 高速EMAが低速EMAを下抜け、RSI < 45、HMA下落中、ボラティリティフィルター通過。
- **インジケーター**: EMA, HMA, RSI, ATR, Highest/Lowest.
- **タイプ**: トレンドフォロー。
- **時間軸**: 短期。
