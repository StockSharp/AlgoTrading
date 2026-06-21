# EMA RSIトレンドリバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI確認によるEMAクロスオーバーでロングエントリーし、RSIがレベルを下回った状態での逆クロスオーバー発生時にエグジットする戦略。パーセントベースのテイクプロフィットとストップロスを使用します。

## 詳細

- **エントリー条件**:
  - ロング: `FastEMA crosses above SlowEMA && RSI > RsiLevel`
- **ロング/ショート**: ロングのみ
- **ストップ**: パーセンテージベースのテイクプロフィットとストップロス
- **デフォルト値**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: EMA, RSI
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
