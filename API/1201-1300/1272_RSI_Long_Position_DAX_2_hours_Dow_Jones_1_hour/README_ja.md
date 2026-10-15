# RSI ロングポジション DAX 2時間足 Dow Jones 1時間足
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI Long Positionは、RSIが売られすぎレベルを上抜けしたときに買いを入れ、RSIがテイクプロフィットレベルを超えるかストップレベルを下抜けたときにクローズします。

## 詳細

- **エントリー条件**: RSIが`Oversold`を上抜ける
- **ロング/ショート**: ロング
- **エグジット条件**: RSIが`TakeProfit`を超えるか、RSIが`StopLoss`を下抜ける
- **ストップ**: いいえ
- **デフォルト値**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: ロング
  - インジケーター: RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
