# 需要ベクトルへの距離戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Distance to Demand Vectorインジケーターに基づく戦略。ロングとショートの需要ベクトルへの距離を比較し、それらのクロスオーバーで取引します。

## 詳細

- **エントリー条件**:
  - ロング: ロングベクトルへの距離 > ショートベクトルへの距離
  - ショート: ロングベクトルへの距離 < ショートベクトルへの距離
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
