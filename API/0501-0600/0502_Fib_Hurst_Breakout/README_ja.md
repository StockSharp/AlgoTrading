# Fib Hurst Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

CandleType は日中足です。Fibonacci 範囲はシグナル当日を除く直前 LookbackPeriod 本の確定 UTC 日足の最高値と最安値で、L+0.618×(H−L)、L+0.382×(H−L) を使います。日次 Hurst は直前 HurstPeriod 終値から H = log(R/S)/log(N)。R は平均からの偏差の累積範囲、S は母標準偏差です。分散ゼロなら入りません。前回と今回の日中終値を同じ現在の日次水準と比較し、61.8% 上抜けかつ H>0.5 で買い、38.2% 下抜けかつ H<0.5 で売ります。空から Volume のみを建て、MaxTradesPerDay は履歴 UTC 日ごとの入場注文試行、MaxTotalTrades は実行全体の試行を数えます。実約定から SL = RiskPercent%、TP = RiskPercent×RiskReward% のネイティブ気配保護を固定します。反対シグナルの反転や待機はありません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | 未指定 |

## 入力データ

任意の `DailyHistoryFile` CSV：`Date,High,Low,Close`。日付は `yyyy-MM-dd`、一意かつ 0 < Low ≤ Close ≤ High。日中シグナル日より前の日付のみを使い、確定日足が自身の日を更新します。将来行は過去の判断に影響しません。

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
