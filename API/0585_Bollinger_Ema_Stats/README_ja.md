# Bollinger EMA Stats戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2つのボリンジャーバンドセットを使用してエントリーおよびストップゾーンを定義し、EMAを出口目標として使用します。

## 詳細
- **エントリー条件**:
  - **ロング**: Close < ボリンジャーバンド下限（エントリー乗数）。
  - **ショート**: Close > ボリンジャーバンド上限（エントリー乗数）。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - EMAでの利益目標。
  - より広いボリンジャーバンドでのストップロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: Bollinger Bands, EMA
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 中期
