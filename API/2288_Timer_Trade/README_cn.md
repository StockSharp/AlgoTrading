# Timer Trade
[English](README.md) | [Русский](README_ru.md)

Timer Trade 策略在固定时间间隔内交替开多单和空单。计时器触发市价订单，并为每个仓位自动设置止损和止盈。

## 详情

- **入场条件**：计时器触发。
- **多/空方向**：双向。
- **出场条件**：止损或止盈。
- **止损**：是，通过 StartProtection。
- **默认参数**：
  - `TimerInterval` = TimeSpan.FromSeconds(30)
  - `Volume` = 1
  - `StopLossLevel` = 10 点
  - `TakeProfitLevel` = 50 点
- **过滤器**：
  - 类别：计时
  - 方向：双向
  - 指标：无
  - 止损：是
  - 复杂度：初级
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
