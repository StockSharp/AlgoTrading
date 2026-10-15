# SMC戦略 BTC 1H OB FVG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

1時間足のBitcoin向けSmart Money Conceptsベースの戦略。上昇トレンドの構造ブレイク後、価格が検出されたオーダーブロックまたはフェアバリューギャップに戻ったときにロングエントリーします。ストップロスはATR乗数を使用し、テイクプロフィットはリスク/リワード比から計算されます。

## 詳細

- **エントリー条件**: 上昇BOS後、`ZoneTimeout`本以内にOBまたはFVGに価格が触れたときに買い。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 固定テイクプロフィットとストップロス。
- **ストップ**: ATRを使用した固定。
- **デフォルト値**:
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: ATR
  - ストップ: 固定
  - 複雑さ: シンプル
  - 時間軸: イントラデイ (1H)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
