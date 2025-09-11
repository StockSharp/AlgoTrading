# G-Channel EMA 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合 G-Channel 通道逻辑与 EMA 趋势过滤。

当最近一次交叉向下且价格低于 EMA 时买入；当最近一次交叉向上且价格高于 EMA 时卖出。

## 详情
- **入场条件**: G-Channel 状态并使用 EMA 过滤。
- **多空方向**: 双向。
- **退出条件**: 反向信号。
- **止损**: 否。
- **默认值**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: G-Channel, EMA
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
