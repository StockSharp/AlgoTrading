# Autostop 策略
[English](README.md) | [Русский](README_ru.md)

用于自动为已有头寸设置止盈和止损的实用策略。
策略本身不生成交易信号，任何在外部打开的仓位都会按照固定距离进行保护。

## 详情

- **入场条件**：无，订单由外部管理。
- **多空方向**：双向。
- **退出条件**：仅防护订单。
- **止损**：通过 StartProtection 设置固定止盈和止损。
- **默认值**：
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **过滤器**：
  - 类别：风险管理
  - 方向：双向
  - 指标：无
  - 止损：固定
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
