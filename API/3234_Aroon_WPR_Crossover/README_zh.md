# Aroon WPR Crossover 策略
[English](README.md) | [Русский](README_ru.md)

趋势跟随策略，将 Aroon 指标的交叉与 Williams %R 动能过滤器结合使用。当 Aroon Up 向上穿越 Aroon Down 且 Williams %R 落入超卖区时开多单；当 Aroon Down 向上穿越 Aroon Up 且 Williams %R 位于超买区时开空单。持仓可在 Williams %R 反向时或触发可选的止盈、止损（以价格步长衡量）时平仓。

## 详细信息

- **入场条件**：
  - 多单：Aroon Up 上穿 Aroon Down 且 Williams %R < `-(100 - OpenWprLevel)`
  - 空单：Aroon Down 上穿 Aroon Up 且 Williams %R > `-OpenWprLevel`
- **交易方向**：双向
- **出场条件**：
  - Williams %R 离开由 `CloseWprLevel` 定义的超买/超卖区
  - 可选的价格步长止盈与止损
- **止损/止盈**：可选的固定价差（价格步长）
- **默认参数**：
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m（关闭）
  - `StopLossSteps` = 0m（关闭）
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **过滤属性**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：Aroon、Williams %R
  - 止损：可选
  - 复杂度：基础
  - 周期：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
