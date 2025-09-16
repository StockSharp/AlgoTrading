# MAM Crossover Trader Strategy
[English](README.md) | [Русский](README_ru.md)

策略对比蜡烛的收盘价和开盘价的简单移动平均线。
当收盘SMA从下向上穿越开盘SMA并且前两根K线确认该突破时开多；反向形态开空。出现反向信号时关闭已有仓位。固定的止损和止盈用于风险控制。

## 详情

- **入场条件**：最近两根K线的SMA(close) 与 SMA(open) 交叉模式。
- **多空方向**：双向。
- **出场条件**：反向交叉或保护性止损。
- **止损**：有。
- **默认参数**：
  - `MaPeriod` = 20
  - `StopLossTicks` = 40
  - `TakeProfitTicks` = 190
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：SMA
  - 止损：固定
  - 复杂度：基础
  - 时间框架：日内（1分）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中
