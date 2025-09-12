# 利弗莫尔-塞科塔突破
[English](README.md) | [Русский](README_ru.md)

该策略结合利弗莫尔枢轴点与塞科塔趋势过滤，并使用ATR进行退出。

测试显示年化收益约87%，在股票市场表现最佳。

策略寻找价格突破最近的枢轴点，同时通过EMA排列和成交量确认趋势方向，并使用ATR止损或移动止损管理风险。

## 详情
- **入场条件**: 价格在趋势和成交量确认下突破最后的枢轴点
- **多空方向**: 双向
- **退出条件**: ATR止损或移动止损
- **止损**: 基于ATR的止损与追踪
- **默认值**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: EMA, 成交量, ATR, Pivot
  - 止损: ATR 追踪
  - 复杂度: 基础
  - 时间框架: 日内 (15m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
