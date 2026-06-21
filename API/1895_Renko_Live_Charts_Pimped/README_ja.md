# Renko Live Charts Pimped 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRenkoブリックを構築し、方向転換時に取引します。オプションでATR値からブリックサイズを計算できるため、Renko構造が市場のボラティリティに適応できます。

## 詳細

- **エントリー条件**:
  - **ロング**: 陰線ブリックの後に陽線のRenkoブリック。
  - **ショート**: 陽線ブリックの後に陰線のRenkoブリック。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `BoxSize` = 10m.
  - `Volume` = 1m.
  - `CalculateBestBoxSize` = false.
  - `AtrPeriod` = 24.
  - `AtrCandleType` = 60m.
  - `UseAtrMa` = true.
  - `AtrMaPeriod` = 120.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Renko, ATR
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: Renko
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
