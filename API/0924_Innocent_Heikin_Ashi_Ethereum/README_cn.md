# Innocent Heikin Ashi Ethereum 策略
[English](README.md) | [Русский](README_ru.md)

当价格在 EMA50 下方出现一系列看跌蜡烛后，若出现一根看涨蜡烛并站上 EMA50，该策略将在以太坊上开多单。止损设在最近 28 根K线的最低点，止盈根据 `RiskReward` 倍数计算。可选的 **Moon Mode** 允许在价格位于 EMA200 之上时进场。仓位也可能因卖出或陷阱信号而提前平仓。

## 细节

- **入场条件**：
  - **做多**：至少 `ConfirmationLevel` 根红色蜡烛在 EMA50 下方，随后一根绿色蜡烛站上 EMA50。
  - **激进**：若启用 `EnableMoonMode` 且价格高于 EMA200。
- **多空方向**：仅多头。
- **出场条件**：
  - 止损设在最近 28 根K线的最低点。
  - 止盈根据 `RiskReward` 倍数计算。
  - 可选的卖出或陷阱信号可提前平仓。
- **止损**：有。
- **默认值**：
  - `RiskReward` = 1。
  - `ConfirmationLevel` = 1。
  - `EnableMoonMode` = true。
- **过滤器**：
  - 类别：Trend following
  - 方向：Long
  - 指标：EMA
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
