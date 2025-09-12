# Supertrend & CCI Strategy Scalp 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用两条 Supertrend 线和一个平滑的 CCI 来捕捉短期反转。
当第一条 Supertrend 在价格上方、第二条在价格下方且平滑 CCI 低于 -100 时买入；做空逻辑相反。

## 细节

- **入场条件**: Supertrend1 在价格上方、Supertrend2 在价格下方、平滑 CCI < -100（做多）；相反条件做空
- **多空方向**: 双向
- **出场条件**: Supertrend 反向排列或 CCI 突破 ±100
- **止损**: 无
- **默认值**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: Supertrend, CCI, Moving Average
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

