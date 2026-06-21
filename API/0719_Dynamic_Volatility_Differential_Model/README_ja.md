# ダイナミック・ボラティリティ・ディファレンシャル・モデル
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Dynamic Volatility Differential Model (DVDM)** 戦略は、インプライド・ボラティリティとヒストリカル・ボラティリティを比較します。インプライド・ボラティリティがリアライズド・ボラティリティをダイナミックな標準偏差閾値分超えたときにロングを建て、スプレッドが負の閾値を下回ったときにショートエントリーします。

シグナルは日次データを使用し、ストップには依存しません。

## 詳細
- **エントリー条件**: ボラティリティ・スプレッドがダイナミック標準偏差閾値を上回る/下回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ボラティリティ・スプレッドがゼロラインを越える。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
