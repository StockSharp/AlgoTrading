# NY ORB CP戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リテスト確認付きNYオープニングレンジブレイクアウト戦略。9:30-9:45のNYレンジのブレイクアウトを取引し、価格がリテストしてブレイクアウト方向を再開するときにエントリーします。

## 詳細

- **エントリー条件**:
  - ロング: ブレイクアウト後にNYの高値をリテストし、トレンドと出来高の確認がある場合。
  - ショート: 下方ブレイクアウト後にNYの安値をリテストし、トレンドと出来高の確認がある場合。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 利益目標: レンジの0.33 * `RiskReward`。
  - ストップロス: レンジの0.33。
- **ストップ**: はい、動的。
- **デフォルト値**:
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: EMA, VWAP, SMA
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
