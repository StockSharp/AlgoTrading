# ダイナミック・ブレイクアウト・マスター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

移動平均トレンドフィルター、RSI・ATRフィルター、出来高・時間制約を備えたドンチャンチャネルを使用したブレイクアウト戦略。

## 戦略ルール

- ロング: 価格がドンチャン上限バンドを上抜けるか、ブレイクアウト後に押し目、MA1 > MA2、RSI が `RsiOversold` と `RsiOverbought` の間、ATR が `AtrMultiplier` 超、出来高が平均超かつ取引時間内。
- ショート: 価格がドンチャン下限バンドを下抜けるか、ブレイクアウト後に戻り、MA1 < MA2、RSI が閾値間、ATR が `AtrMultiplier` 超、出来高が平均超かつ取引時間内。
- エグジット: ストップロス/トレーリング、利益確定、RSI 極値、または移動平均クロスオーバー。

## パラメーター

- `DonchianPeriod` – チャネルのルックバック期間。
- `Ma1Length`, `Ma1IsEma` – 第1移動平均。
- `Ma2Length`, `Ma2IsEma` – 第2移動平均。
- `RsiLength`, `RsiOverbought`, `RsiOversold` – RSI フィルター。
- `AtrLength`, `AtrMultiplier` – ボラティリティフィルター。
- `RiskPerTrade`, `RewardRatio`, `AccountSize` – ポジションサイジング。
- `TradingStartHour`, `TradingEndHour` – 取引セッション。
- `CandleType` – ローソク足の時間軸。
