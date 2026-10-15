# KumoTrade Ichimoku戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Ichimoku CloudとStochastic Oscillatorに基づく戦略。
Stochasticが売られすぎで雲が前方にない状態でKijunの上に価格が戻るとロングエントリー。
Stochasticが買われすぎで弱気なKumoがある状態で価格が雲の下に落ちるとショートエントリー。

## 詳細

- **エントリー条件**:
  - ロング: `Low > Kijun && Kijun > Tenkan && Close < SenkouA && StochD < 29`
  - ショート: `Close < min(SenkouA, SenkouB) && High > Kijun && prevStochD > StochD >= 90`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ATRベースのトレーリングストップ
- **ストップ**: ATR * 3を使用したトレーリングストップ
- **デフォルト値**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouPeriod` = 52
  - `StochK` = 70
  - `StochD` = 15
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Ichimoku Cloud, Stochastic, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
