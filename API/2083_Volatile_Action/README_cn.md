# 波动行动策略

该策略结合了短期波动性突破和比尔·威廉姆斯的 **Alligator** 趋势过滤器，Alligator 在4小时周期上计算。

## 交易规则
- **做多** 条件：
  - 周期为1的ATR大于 *Volatility Coef* 与周期为 *ATR Period* 的ATR的乘积。
  - K线收阳并且创下最近24根K线的新高。
  - Alligator三条线向上排列（Lips > Teeth > Jaw），且开盘价和收盘价都在Teeth线上方。
- **做空** 条件：以上条件反向满足。

开仓后根据ATR(1)设置止损和止盈：
- 止损 = 入场价 ± *Stop Coef* × ATR(1)
- 止盈 = 入场价 ± *Profit Coef* × ATR(1)

## 参数
- **Volatility Coef** – 快速ATR与慢速ATR的比较系数。
- **ATR Period** – 慢速ATR的周期。
- **Stop Coef** – 用于止损的ATR倍数。
- **Profit Coef** – 用于止盈的ATR倍数。
- **Candle Type** – 主要分析的时间框架（Alligator 使用4小时K线）。
