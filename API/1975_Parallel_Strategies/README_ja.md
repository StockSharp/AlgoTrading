# 並列戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

両方向でトレードするHeikin Ashi MACDブレイクアウトシステムです。新しいHeikin Ashiトレンドがドンチャンチャネルの上下へのブレイクアウトと一致し、MACDがモメンタムを確認したときにエントリーします。

Heikin Ashiによるトレンド識別とブレイクアウト検出を組み合わせることで、トレードを新鮮な動きに合わせた状態に保ちます。MACDはダマシシグナルを避けるためのモメンタムフィルターとして機能します。

トレンド反転後の早期ブレイクアウトエントリーを探すトレーダーに最適です。イントラデイの時間軸で機能します。

## 詳細

- **エントリー条件**:
  - ロング: `Trend turns bullish && Close > DonchianHigh && MACD > Signal`
  - ショート: `Trend turns bearish && Close < DonchianLow && MACD < Signal`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対のブレイクアウトシグナル
- **ストップ**: 未定義
- **デフォルト値**:
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Heikin Ashi, Donchian Channel, MACD
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
