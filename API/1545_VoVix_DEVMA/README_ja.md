# VoVix DEVMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はATRの標準偏差を基に構築した偏差移動平均（DEVMA）を使ってボラティリティの挙動を分析します。収縮と拡張のレジーム転換をトレードし、ATRベースのエグジットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: 高速DEVMAが低速DEVMAを上抜けする。
  - **ショート**: 高速DEVMAが低速DEVMAを下抜けする。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - ATRのストップロスとテイクプロフィット。
- **ストップ**: あり、ATR乗数。
- **デフォルト値**:
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 複雑
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
