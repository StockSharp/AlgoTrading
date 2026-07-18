# The MasterMind 2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は**Stochastic Oscillator**と**Williams %R**を組み合わせて、極端な売られすぎおよび買われすぎの状態を識別します。
Stochasticのシグナルラインが**3**を下回り、かつWilliams %Rが**-99.9**未満のときにロングポジションを建てます。
Stochasticのシグナルラインが**97**を上回り、かつWilliams %Rが**-0.1**超のときにショートポジションを建てます。

リスク管理には、初期ストップロスとテイクプロフィット、調整可能なステップのトレーリングストップ、十分な利益を得た後にストップをエントリー価格に移動するオプションのブレークイベントリガーが含まれます。

## パラメーター

- `LotSize` - 取引量（コントラクト数）。
- `StochasticPeriod` - Stochastic Oscillatorの期間。
- `StochasticK` - %Kラインの平滑化。
- `StochasticD` - %D（シグナル）ラインの平滑化。
- `WilliamsRPeriod` - Williams %Rの期間。
- `StopLossPoints` - 初期ストップロス（価格ポイント）。
- `TakeProfitPoints` - 初期テイクプロフィット（価格ポイント）。
- `TrailingStopPoints` - トレーリングストップの距離（ポイント）。
- `TrailingStepPoints` - トレーリングストップ更新前の最小有利な動き。
- `BreakEvenPoints` - ストップをブレークイーブンに移動するための距離（ポイント）。
- `CandleType` - 計算に使用するローソク足のタイプとタイムフレーム。

## トレードロジック

1. **エントリー条件**
   - Stochasticシグナル < 3 かつ Williams %R < -99.9 のとき**買い**。
   - Stochasticシグナル > 97 かつ Williams %R > -0.1 のとき**売り**。
2. **エグジット条件**
   - 逆のエントリーシグナルで既存ポジションをクローズ。
   - ストップロス、テイクプロフィット、ブレークイーブン、トレーリングストップは毎ローソク足で適用。

## 注記

- 必要なインジケーターをサポートするあらゆる銘柄で機能します。
- 教育目的およびさらなる実験のために設計されています。
