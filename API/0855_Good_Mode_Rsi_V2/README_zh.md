# Good Mode RSI v2 策略
[English](README.md) | [Русский](README_ru.md)

本策略利用 RSI 指标的极端值进行交易，并结合获利水平与追踪止损。当 RSI 超过设定的卖出阈值时开空仓，在 RSI 下降至获利目标时平仓。当 RSI 低于设定的买入阈值时开多仓，在 RSI 上升至获利目标时平仓。无论多空方向，追踪止损都会跟随价格的最佳运行以保护利润。

## 详情

- **入场条件**:
  - **多头**：`RSI < buy level`
  - **空头**：`RSI > sell level`
- **多/空**：双向
- **出场条件**:
  - **多头**：`RSI > take profit level buy` 或触发追踪止损
  - **空头**：`RSI < take profit level sell` 或触发追踪止损
- **止损**：以跳数计算的追踪止损
- **默认值**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **过滤器**:
  - 分类: 动量
  - 方向: 双向
  - 指标: 单一
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险水平: 中等
