# Supertrend & CCI スキャルプ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrend & CCI スキャルプ戦略は、2本のSupertrend ラインと平滑化されたCCIを使用して短期リバーサルを捉えます。
最初のSupertrendが価格より上、2番目が価格より下にあり、平滑化CCI が-100以下のときに買います。ショートのロジックはこの逆です。

## 詳細

- **エントリー条件**: Supertrend1が価格より上、Supertrend2が価格より下、平滑化CCI < -100（ロング）; ショートはその逆
- **ロング/ショート**: 両方
- **エグジット条件**: Supetrendの逆方向アライメントまたはCCIが±100をクロス
- **ストップ**: いいえ
- **デフォルト値**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Supertrend, CCI, Moving Average
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

