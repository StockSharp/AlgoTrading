# IU Bigger Than Range 策略
[English](README.md) | [Русский](README_ru.md)

当当前K线实体大于之前区间时入场的突破策略。

系统比较当前K线实体与设定周期内开收价的最高和最低范围。如果实体超过前一范围，则按K线方向开仓，并根据所选方法设置止损和止盈。

## 细节

- **入场条件**：K线实体大于前一范围；方向取决于K线实体。
- **多空方向**：双向。
- **出场条件**：止损或止盈触发。
- **止损**：前一K线、ATR或摆动高/低。
- **默认值**：
  - `LookbackPeriod` = 22
  - `RiskToReward` = 3
  - `StopLossMethod` = PreviousHighLow
  - `AtrLength` = 14
  - `AtrFactor` = 2m
  - `SwingLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：突破
  - 方向：双向
  - 指标：Highest, Lowest, ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
