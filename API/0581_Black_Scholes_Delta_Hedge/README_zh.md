# Black-Scholes Delta 对冲
[English](README.md) | [Русский](README_ru.md)

该策略使用 Black-Scholes 模型计算期权的理论价格和 Delta，并在设定的间隔内通过交易标的资产来对冲 Delta。

## 细节
- **功能**: 基于 Black-Scholes 定价的 Delta 对冲
- **参数**: Strike Price, Risk Free Rate, Volatility, Days To Expiry, Option Type, Position Side, Position Size, Hedge Interval, Candle Type
- **指标**: 无
- **多空**: 取决于持仓方向
- **止损**: 无
