# Simple MA ADX EA
[English](README.md) | [Русский](README_ru.md)

该策略结合EMA与平均趋向指数(ADX)来确认趋势强度。

当EMA上升、上一根K线收盘价高于EMA、ADX超过阈值且+DI大于-DI时买入。相反条件出现时卖出。策略使用止损和止盈进行风险管理。

## 详情

- **入场条件**: EMA方向、价格与EMA、ADX、+DI/-DI。
- **多空方向**: 双向。
- **退出条件**: 反向信号或保护单。
- **止损**: 是。
- **默认值**:
  - `AdxPeriod` = 8
  - `MaPeriod` = 8
  - `AdxThreshold` = 22m
  - `StopLoss` = 30m
  - `TakeProfit` = 100m
  - `Volume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: EMA, ADX
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

