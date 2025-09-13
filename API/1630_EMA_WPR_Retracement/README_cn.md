# EMA WPR Retracement 策略
[English](README.md) | [Русский](README_ru.md)

基于 EMA 趋势过滤和 Williams %R 极值的趋势跟随策略。每次开仓前，Williams %R 必须从极端区域回撤，从而允许新的交易，并支持金字塔式加仓。

## 细节

- **入场条件**：
  - **多头**：Williams %R 低于 -100 后回撤至 `WPR Retracement` 以上，可选的 EMA 上升趋势确认。
  - **空头**：Williams %R 高于 0 后回撤至 `-WPR Retracement` 以下，可选的 EMA 下降趋势确认。
- **多空方向**：支持双向并可加仓。
- **出场条件**：
  - Williams %R 离开极值区域。
  - 可选地，在 `Max Unprofit Bars` 根无盈利 K 线后退出。
  - 保护模块管理止损、止盈及可选的跟踪止损。
- **止损**：固定止损和止盈，可选跟踪止损。
- **默认参数**：
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA, Williams %R
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
