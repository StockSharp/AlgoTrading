[Русский](README_ru.md) | [English](README.md)

Turtle Trader V1 将多种动量振荡指标与移动平均过滤器结合。 当快速 EMA 高于慢速 EMA 并且 RSI、随机指标、CCI、动量和 Chaikin 振荡器全部向上时开多单。 空头需要相反条件。

## 细节

- **入场条件**:
  - 快速 EMA 高于慢速 EMA（空头反之）
  - 多头 RSI 上升且低于 70，空头 RSI 下降且高于 30
  - 多头随机指标 %K 低于 88，空头高于 12
  - 多头 CCI 与动量上升，空头下降
  - Chaikin 振荡器朝交易方向移动
- **多空**: 双向
- **离场条件**: 反向信号
- **止损**: 默认无
- **默认参数**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **过滤器**:
  - 类别: 趋势/动量
  - 方向: 双向
  - 指标: EMA、RSI、随机指标、CCI、动量、Chaikin 振荡器
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 1 小时
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
