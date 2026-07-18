# Trix Candle戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Trix Candleインジケーターに基づいてリバーサルを取引します。このインジケーターはローソク足の始値と終値に三重指数移動平均を適用し、平滑化された終値が平滑化された始値より上か下かによって各ローソク足を着色します。

## 詳細

- **エントリー条件**:
  - **ロング**: 前のローソク足が強気（色2）かつ現在のローソク足の色 < 2
  - **ショート**: 前のローソク足が弱気（色0）かつ現在のローソク足の色 > 0
- **ロング/ショート**: ロングとショート
- **エグジット条件**:
  - ロング: 前のローソク足が弱気（色0）
  - ショート: 前のローソク足が強気（色2）
- **ストップ**: いいえ
- **デフォルト値**:
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Triple Exponential Moving Average
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
