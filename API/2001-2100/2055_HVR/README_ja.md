# HVR（ヒストリカル・ボラティリティ・レシオ）戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Historical Volatility Ratio (HVR) に基づく戦略。対数リターンを使用して、6本のバーの短期ボラティリティと100本のバーの長期ボラティリティを比較します。レシオが閾値を上回ると、ボラティリティ拡大を期待してロングに入ります。閾値を下回ると、ショートに入ります。

## 詳細

- **エントリー条件**:
  - ロング: `HVR > RatioThreshold`
  - ショート: `HVR < RatioThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `ShortPeriod` = 6
  - `LongPeriod` = 100
  - `RatioThreshold` = 1.0
  - `CandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: ヒストリカル・ボラティリティ（短期・長期）
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
