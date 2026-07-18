# SuperTrade ST1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SupertrendをEMAフィルターとATRベースのリスク管理と組み合わせたロングオンリー戦略です。

テスト結果は年平均リターン約45%を示しています。暗号通貨市場で最も良いパフォーマンスを発揮します。

価格がSupertrendラインとEMAの両方を上回っている間に、Supertrend方向の低下を待ちます。リスクはATRベースのストップロスと利益確定レベルで1:4の比率にてコントロールされます。

## 詳細

- **エントリー条件**:
  - 直前のSupertrendの方向 > 現在の方向
  - 終値 > Supertrend
  - 終値 > EMA
- **ロング/ショート**: ロングのみ
- **エグジット条件**: `Close <= entry - StopAtrMultiplier * ATR` または `Close >= entry + TakeAtrMultiplier * ATR`
- **ストップ**: ATRベースのストップロスと利益確定
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `EmaPeriod` = 200
  - `StopAtrMultiplier` = 1.0
  - `TakeAtrMultiplier` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: Supertrend, EMA, ATR
  - ストップ: はい
  - 複雑さ: シンプル
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

