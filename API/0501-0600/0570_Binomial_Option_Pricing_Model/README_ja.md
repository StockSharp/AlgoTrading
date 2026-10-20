# Binomial Option Pricing Model

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 評価ルール

取引注文を出さない、2 段階 Cox–Ross–Rubinstein 方式のオプション評価モジュールです。`CalculatePrice(spot, volatility, timeToExpiry)` は設定済み行使価格、コール/プット、欧州/米国方式、資産区分に対して価格を返します。金利とボラティリティは年率の小数で、`0.05` は 5%。1 年は 365 日とし、分・時間の残存期間にも対応します。

`dt = T / 2` に対し `u = exp(volatility * sqrt(dt))`、`d = 1/u`、`p = (exp(b * dt) - d) / (u - d)`。`Stock`、`Index`、`Currency` は `b = r - q`（q は配当利回り/外国金利）、`Future` は `b = 0`。各後退段階を `exp(-r * dt)` で割り引きます。欧州方式は満期のみ、米国方式は各節点で継続価値と本源的価値を比較します。期間ゼロは本源的価値、変動ゼロは確定的なキャリー経路で同じ行使規則を適用します。未対応区分・方式や不正確率は値を丸めず明示的に拒否します。

`EvaluatePrice(candle)` は各確定足を一度だけ処理します。`Length` 個の終値から `母標準偏差(close) / mean(close) * sqrt(365 日 / 足間隔)` で年率変動を推定します。`TimeToExpiry` を評価し、`LastOptionPrice` を更新、ログを出し、チャートがあれば足と偏差を描きます。これは価格水準のばらつきであり、リターン変動推定や売買シグナルではありません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `StrikePrice` | `100` |
| `DividendYield` | `0` |
| `AssetClass` | `Stock` |
| `OptionStyle` | `European` |
| `OptionType` | `Call` |
| `TimeToExpiry` | `30d` |
| `Length` | `20` |
| `CandleType` | `1m` |
| `RiskFreeRate` | `0.05` |
