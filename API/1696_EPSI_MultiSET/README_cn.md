# EPSI Multi SET 策略
[English](README.md) | [Русский](README_ru.md)

从原始 MQL4 专家 *e-PSI@MultiSET* 转换而来的突破策略。
监控每根蜡烛，当价格从开盘价移动到指定距离时入场。
仓位使用止盈和止损保护，交易仅在用户设定的时间窗口内进行。

## 细节

- **入场条件**：
  - 多头：`High - Open >= MinDistance`
  - 空头：`Open - Low >= MinDistance`
- **多/空**：双向
- **出场条件**：TakeProfit 或 StopLoss
- **止损**：有
- **默认参数**：
  - `MinDistance` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 200
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `OpenHour` = 2
  - `CloseHour` = 20
- **过滤器**：
  - 类别：Breakout
  - 方向：双向
  - 指标：无
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
