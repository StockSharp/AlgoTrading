# 自适应市场水平
[English](README.md) | [Русский](README_ru.md)

基于自适应市场水平（Adaptive Market Level，AML）指标的策略。该指标根据当前波动率动态调整价格水平。当AML线转向上时开多单，转向下时开空单。出现反向颜色变化或触发止损/止盈时平仓。

系统跟随中期趋势，默认在较高时间框架上运行。

## 细节

- **入场条件**：AML线向上转折做多，向下转折做空。
- **多空方向**：双向。
- **退出条件**：AML方向改变或到达止损/止盈。
- **止损**：有。
- **默认参数**：
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: Adaptive Market Level
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: H4
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
