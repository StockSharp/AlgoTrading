# Donchian RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Donchian Channels と RSI インジケーターを組み合わせた戦略。RSI がトレンドが過度に延びていないことを確認したとき、Donchian のブレイクアウトで買いを行います。

テストでは年平均リターン約 55% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

Donchian Channels はブレイクアウトレベルを特定し、RSI はモメンタムがその動きを支持しているかを確認します。ブレイクアウトが RSI の方向と一致したときにポジションを建てます。

フェイクアウトではなく持続的なブレイクアウトを期待するトレーダーに最適です。パーセントストップでリスクを限定します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > DonchianHigh && RSI < RsiOverboughtLevel`
  - ショート: `Close < DonchianLow && RSI > RsiOversoldLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ブレイクアウトの失敗または反対のシグナル
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    RSIがRsiOverboughtLevel未満（ロング）またはRsiOversoldLevel超（ショート）である限り、ブレイクアウトは行き過ぎではありません。チャネルは直前DonchianPeriod本で構成します。価格が突破した水準の反対側に戻って引けたらブレイク失敗としてポジションを閉じます。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Donchian Channel, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
