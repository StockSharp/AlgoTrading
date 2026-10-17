# RSI動的買われすぎ/売られすぎ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型の買われ過ぎ・売られ過ぎ境界と価格トレンドフィルターで RSI の極値を売買します。確定5分足で `R = RSI(14)`、終値の `P = SMA(50)`、R の `A = SMA(50)` と `D = StandardDeviation(50)` を計算します。境界は `upper = min(A + 2 × D, 85)`、`lower = max(A − 2 × D, 15)`。全指標の形成が必要です。

建玉なしで `R <= lower && Close >= P × 0.995` なら買い、`R >= upper && Close <= P × 1.005` なら売りです。`R >= A || Close < P × 0.995` で全ロング、`R <= A || Close > P × 1.005` で全ショートを決済します。各シグナル注文後、`CooldownBars = 48` 本の確定足の間、次のシグナル判断を待ちます。

約定済み建玉には実際の入場約定価格から `StopLossPercent = 2` のローカル標準パーセント保護が働きます。銘柄の bid/ask を監視し、シグナル待機期間と独立して成行決済します。足の間の保護には気配配信が必要です。利確は設定しません。

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
