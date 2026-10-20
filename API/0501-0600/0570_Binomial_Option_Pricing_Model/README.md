# Binomial Option Pricing Model

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Valuation rules

A nontrading two-step Cox–Ross–Rubinstein option valuation module. It never submits orders. `CalculatePrice(spot, volatility, timeToExpiry)` returns a price for configured strike, call/put, European/American exercise and asset class. Rates and volatility are annual fractions: `0.05` means 5%. Time is converted to years of 365 days; minutes and hours are supported.

For `dt = T / 2`, set `u = exp(volatility * sqrt(dt))`, `d = 1/u`, `p = (exp(b * dt) - d) / (u - d)`. Carry `b = r - q` for `Stock`, `Index` and `Currency` (q is dividend yield/foreign rate); `b = 0` for `Future`. Discount each backward step by `exp(-r * dt)`. European options exercise at maturity; American options compare continuation with intrinsic value at every node. At zero tenor return intrinsic value; zero volatility follows the deterministic carry path with the same exercise rules. Unsupported classes/styles and invalid probabilities fail explicitly, without clamping.

`EvaluatePrice(candle)` processes each finished source candle once. After `Length` closes, estimate annual volatility as `population SD(close) / mean(close) * sqrt(365 days / candle interval)`. Price the configured `TimeToExpiry`, expose `LastOptionPrice`, log the value and plot source candles/deviation where a chart is attached. This is a rolling price-level dispersion estimate, not a return-based volatility estimator or an execution signal.

## Parameters

| Parameter | Default |
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
