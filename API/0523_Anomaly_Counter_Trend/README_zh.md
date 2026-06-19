# 异常逆势策略
[English](README.md) | [Русский](README_ru.md)

该算法检测短时间内的剧烈百分比变动并逆向交易。价格大幅上升超过阈值时做空，跌破阈值时做多。止损和止盈以跳数设置。

## 详情

- **入场条件**: 价格在回溯窗口内的百分比变动超过阈值。
- **多空方向**: 双向。
- **出场条件**: 止损或止盈。
- **止损**: 有。
- **默认值**:
  - `PercentageThreshold` = 1
  - `LookbackMinutes` = 30
  - `StopLossTicks` = 100
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**:
  - 类别: 逆势
  - 方向: 双向
  - 指标: 价格
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
