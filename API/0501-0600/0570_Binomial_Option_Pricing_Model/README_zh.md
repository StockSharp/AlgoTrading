# Binomial Option Pricing Model

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 定价规则

不交易的两步 Cox–Ross–Rubinstein 期权定价模块，不提交任何订单。`CalculatePrice(spot, volatility, timeToExpiry)` 按配置的执行价、看涨/看跌、欧式/美式及资产类别返回价格。利率和波动率为年化小数，`0.05` 表示 5%。一年按 365 天，期限也支持分钟和小时。

`dt = T / 2`，`u = exp(volatility * sqrt(dt))`、`d = 1/u`、`p = (exp(b * dt) - d) / (u - d)`。`Stock`、`Index`、`Currency` 使用 `b = r - q`（q 为股息率或外币利率），`Future` 使用 `b = 0`。每次向后计算乘以 `exp(-r * dt)`。欧式到期行权；美式在每个节点比较继续持有和内在价值。零期限返回内在价值；零波动率按确定性持有成本路径并遵循相同行权规则。未支持的类别/风格及非法概率明确报错，不强制截断。

`EvaluatePrice(candle)` 对每根已完成足只处理一次。得到 `Length` 个收盘后，以总体 `SD(close) / mean(close) * sqrt(365 天 / 足间隔)` 估计年化波动率。计算配置的 `TimeToExpiry`，更新 `LastOptionPrice`、记录日志，有图表时绘制价格足及标准差。这是价格水平的滚动离散度估计，不是收益率波动率估计或交易信号。

## 参数

| 参数 | 默认值 |
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
