# 二项期权定价模型
[English](README.md) | [Русский](README_ru.md)

该策略使用两步二项树计算期权的理论价格，支持美式或欧式、看涨或看跌以及不同资产类别。波动率通过收盘价的标准差估计。

策略不产生任何交易信号，只是在每根完成的K线输出计算得到的期权价格。

## 细节
- **功能**: 期权定价（无交易）
- **参数**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **指标**: Standard Deviation
- **多/空**: 无
- **止损**: 无
