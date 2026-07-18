# Fib Hurstブレイクアウト
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fib Hurstブレイクアウトは、日足時間軸のフィボナッチリトレースメントレベルとHurst指数フィルターを組み合わせます。優勢なトレンド方向への主要フィボナッチレベルの価格突破がエントリーをトリガーし、2%ストップと1:2のリスクリワードでリスクを管理します。

## 詳細

- **エントリー条件**:
  - ロング: 終値が61.8%レベルを上抜けかつ日足Hurst > 0.5
  - ショート: 終値が38.2%レベルを下抜けかつ日足Hurst < 0.5
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: はい
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Hurst, Fibonacci
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
