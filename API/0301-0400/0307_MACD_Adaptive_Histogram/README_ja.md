# MACDアダプティブヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

統計的に大きな MACD ヒストグラム値を売買します。確定15分足で `H = MACD(12,26) − Signal(9)` を用い、H の `A = SMA(20)` と `D = StandardDeviation(20)` を計算します。全指標の形成と `D > 0` を待ちます。

建玉なしで `H >= A + 2 × D && H > 0` なら買い、`H <= A − 2 × D && H < 0` なら売りです。`H <= A` で全ロング、`H >= A` で全ショートを決済します。各シグナル注文後、`CooldownBars = 16` 本の確定足の間、次のシグナル判断を待ちます。

約定済み建玉には実際の入場約定価格から `StopLossPercent = 2` のローカル標準パーセント保護が働きます。銘柄の bid/ask を監視し、シグナル待機期間と独立して成行決済します。足の間の保護には気配配信が必要です。利確は設定しません。

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
