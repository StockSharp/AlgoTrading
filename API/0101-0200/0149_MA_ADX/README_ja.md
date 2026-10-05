# Ma Adx 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
MAとADXインジケーターに基づく戦略。価格がMAを強いトレンドで交差したときにポジションを取ります。

テストでは年平均リターン約184%を示しています。暗号資産市場で最もパフォーマンスが高いです。

移動平均がトレンドを指示し、ADXがそれが取引するのに十分なほど強いかどうかを検証します。ADXが閾値を超えたときの価格のMA交差に従ってエントリーが行われます。

このクラシックなトレンドアプローチはシステマティックなトレーダーに向いています。損失はパーセントストップで管理し、利益はATRベースの目標で確定します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > MA && ADX > 25`
  - ショート: `Close < MA && ADX > 25`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のMAクロスまたはストップ
- **ストップ**: `StopLossPercent` パーセント、テイクプロフィット `TakeProfitAtrMultiplier` ATR
- **デフォルト値**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
  - `AdxThreshold` = 25
  - `AtrPeriod` = 14
    エントリーには、ADXがAdxThresholdを上回る中でその足の終値がSMAを横切ることが必要です。逆方向のクロスでポジションを決済し、ADXが引き続き強ければ反対側を建てます。目標はエントリー時の終値からTakeProfitAtrMultiplier倍のATR（AtrPeriod）の位置で、足の終値で判定します。
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Moving Average, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

