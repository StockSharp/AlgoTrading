# Fisher Transform X2 策略
[English](README.md) | [Русский](README_ru.md)

该策略在两个不同的时间框架上使用 Fisher Transform 指标。高时间框架确定总体趋势，低时间框架在 Fisher 与其前值相反方向交叉时产生入场信号。可选参数允许在趋势变化或交叉信号时平仓。

## 详情

- **入场条件**:
  - **多头**: `趋势 Fisher 上升` && `信号 Fisher 向下穿越其前值`
  - **空头**: `趋势 Fisher 下降` && `信号 Fisher 向上穿越其前值`
- **多空方向**: 双向
- **出场条件**:
  - 可选的趋势反转平仓
  - 可选的信号时间框架反向交叉平仓
- **止损**: 以点数设置的止盈和止损
- **默认值**:
  - `Trend Length` = 10
  - `Signal Length` = 10
  - `Trend Timeframe` = 6 小时
  - `Signal Timeframe` = 30 分钟
  - `Take Profit` = 2000 点
  - `Stop Loss` = 1000 点
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: Fisher Transform
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 多时间框架
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
