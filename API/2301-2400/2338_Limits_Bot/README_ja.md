# Limits Bot 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

各ローソク足の始値周辺に対称的なリミット注文を置き、ストップロス、テイクプロフィット、オプションのトレーリングでポジションを保護します。

## 詳細

- **エントリー**:
  - ロング取引が有効な場合、`Open - StopOrderDistance * PriceStep` で買いリミット注文。
  - ショート取引が有効な場合、`Open + StopOrderDistance * PriceStep` で売りリミット注文。
- **エグジット**: ストップロス、テイクプロフィット、またはトレーリングストップが発動したとき成行でクローズ。
- **ロング/ショート**: 両方。
- **ストップ**: トレーリングオプション付き固定ストップロス。
- **デフォルト値**:
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1分
- **セッション**: `StartTime` から `EndTime` の間のみ取引します。
- **フィルター**:
  - カテゴリ: Price action
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
