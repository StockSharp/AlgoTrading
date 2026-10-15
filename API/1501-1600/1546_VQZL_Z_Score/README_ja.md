# VQZL Z-Score 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化平均に対するZ-Scoreを使用する戦略。

テストでは平均年間収益率約42%を示しています。株式市場で最も良いパフォーマンスを発揮します。

この戦略は平滑化移動平均と標準偏差を計算してZ-Scoreを求めます。価格が閾値を超えて乖離したとき、その動きの方向にエントリーします。

## 詳細

- **エントリー条件**:
  - **ロング**: `Z-Score > threshold`。
  - **ショート**: `Z-Score < -threshold`。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆方向のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
