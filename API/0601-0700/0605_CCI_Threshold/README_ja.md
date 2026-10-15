# CCI しきい値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

CCIがしきい値を下回ったときに買い、終値が前の終値を上回ったときにエグジットする戦略です。
絶対ポイントによるオプションのストップロスとテイクプロフィットがあります。

## 詳細

- **エントリー条件**:
  - ロング: `CCI < BuyThreshold`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `ClosePrice > previous ClosePrice`
- **ストップ**: `UseStopLoss` と `UseTakeProfit` によるオプション
- **デフォルト値**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: CCI
  - ストップ: オプション
  - 複雑さ: 低
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
