# Panel Joke 策略
[English](README.md) | [Русский](README_ru.md)

该策略是将 MetaTrader 的 *panel-joke* 系统转换为 StockSharp。它比较当前和上一根 K 线的七个价格指标（开盘价、高点、低点、high/low 平均值、收盘价、high/low/close 平均值以及 high/low/close 加权平均值）。每个上涨的指标会增加买入计数器，每个下降的指标会增加卖出计数器。

当 `Enable Autopilot` 参数为 `true` 时，策略会根据计数器的高低自动开仓或反向开仓，不使用额外的指标或止损规则。

## 详情

- **入场条件**：
  - **做多**：Buy counter > Sell counter。
  - **做空**：Sell counter > Buy counter。
- **出场条件**：出现相反信号时反向。
- **止损**：无。
- **默认值**：
  - `Enable Autopilot` = `true`。
  - `Candle Type` = 5 分钟。
- **筛选**：
  - 类别：Price action
  - 方向：双向
  - 指标：无
  - 止损：否
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：高

