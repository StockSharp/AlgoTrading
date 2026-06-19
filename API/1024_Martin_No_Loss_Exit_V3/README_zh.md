# Martin Strategy - No Loss Exit V3
[English](README.md) | [Русский](README_ru.md)

该马丁加仓策略在价格从首单下跌指定百分比时继续做多，并按系数增加投入金额，重新计算平均成本。当蜡烛最高价达到平均成本加上止盈百分比时平仓，从而确保只在盈利时退出。

## 详情

- **入场条件**:
  - **多头**: `空仓` → 按 `Initial Cash` 买入
  - **加仓**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` 且 `orderCount < MaxOrders`
- **多空方向**: 仅多头
- **出场条件**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **止损**: 无
- **默认值**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **筛选**:
  - 类别: 补仓
  - 方向: 多头
  - 指标: 无
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 高
