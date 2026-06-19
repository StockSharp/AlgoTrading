# Coensio Swing Trader 策略
[English](README.md) | [Русский](README_ru.md)

基于用户定义趋势线的突破策略。通过参数提供的斜率和截距计算多空趋势线。当收盘价高于买入线加上阈值时开多单；当收盘价低于卖出线减去阈值时开空单。

仓位使用以跳动点为单位的止盈和止损保护。可选的跟踪止损在价格向有利方向移动时调整保护性止损。还可以在下一根K线确认假突破时平仓。

## 详情

- **入场条件**:
  - 多头：`Close > BuyLine + EntryThreshold`
  - 空头：`Close < SellLine - EntryThreshold`
- **多/空**：均可
- **出场条件**：止损、止盈、跟踪止损或反向信号
- **止损**:
  - 以跳动点为单位的止盈
  - 以跳动点为单位的止损
  - 可选的跟踪止损
  - 可选的假突破平仓
- **默认值**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **过滤器**:
  - 分类：趋势线突破
  - 方向：双向
  - 指标：无
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
