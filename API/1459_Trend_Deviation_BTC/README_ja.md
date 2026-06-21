# トレンド乖離戦略 BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DMI クロスと Bollinger Bands、そして Momentum、MACD、SuperTrend、Aroon の確認を組み合わせます。トレンド内での価格乖離を探し、複数のシグナルが一致したときにエントリーします。

## 詳細

- **エントリー条件**: +DI が -DI を上抜け、価格が Bollinger 上限バンドを下回り、Momentum/MACD/SuperTrend/Aroon のいずれかが確認。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
