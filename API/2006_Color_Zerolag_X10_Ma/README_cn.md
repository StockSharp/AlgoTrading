# Color Zerolag X10 MA 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 示例 **Exp_ColorZerolagX10MA.mq5** 的简化移植版本。它利用零滞后指数移动平均线 (Zero Lag EMA) 来检测斜率变化。当均线在连续两根柱子下降后转而向上时，策略会开多或反手做多；反之，当均线在上升后转而向下时，策略会开空。

原始系统依赖由十条平滑均线构成的复杂指标。此处使用 StockSharp 内置的 `ZeroLagExponentialMovingAverage` 代替，使实现更加简洁。策略在选定的K线时间框架上运行，并可通过参数启用或禁用各个动作（开/平多空）。

## 细节

- **入场条件**：
  - **多头**：`ZLEMA[t-2] > ZLEMA[t-1]` 且 `ZLEMA[t] > ZLEMA[t-1]`。
  - **空头**：`ZLEMA[t-2] < ZLEMA[t-1]` 且 `ZLEMA[t] < ZLEMA[t-1]`。
- **多空方向**：双向。
- **出场条件**：
  - 当出现空头信号且启用 `BuyPosClose` 时平多。
  - 当出现多头信号且启用 `SellPosClose` 时平空。
- **止损**：默认无，依靠反向信号退出。
- **默认值**：
  - `Length` = 20。
  - `CandleType` = 4 小时时间框架。
  - 所有动作标志 (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`) 启用。
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：单一
  - 止损：否
  - 复杂度：简单
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
