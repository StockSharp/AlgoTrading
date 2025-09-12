# 时间段过滤器 - MACD示例
[Русский](README_ru.md) | [English](README.md)

该策略展示如何结合时间段过滤器、MACD和趋势EMA，仅在指定时间内交易。

## 细节

- **入场条件**：在活跃交易时段内，MACD与信号线交叉并考虑价格相对趋势EMA。
- **多/空**：双向。
- **出场条件**：反向交叉或在启用选项时会话结束。
- **止损**：无。
- **默认参数**：
  - `SessionStart` = 11:00
  - `SessionEnd` = 15:00
  - `CloseAtSessionEnd` = false
  - `FastEmaPeriod` = 11
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `TrendMaLength` = 55
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: MACD, EMA
  - 止损: 无
  - 复杂度: 中等
  - 周期: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
