# 护航趋势策略
[English](README.md) | [Русский](README_ru.md)

护航趋势策略结合快慢加权移动平均线（WMA）、MACD 和 CCI 共同确认信号。当快线 WMA 位于慢线之上、MACD 主线在信号线上方且 CCI 超过正阈值时开多头；相反条件满足时开空头。策略可选用固定止损、止盈和移动止损。

## 详情
- **入场条件**：
  - **多头**：`FastWMA > SlowWMA` 且 `MACD > Signal` 且 `CCI > +Threshold`。
  - **空头**：`FastWMA < SlowWMA` 且 `MACD < Signal` 且 `CCI < -Threshold`。
- **多空方向**：双向。
- **退出条件**：
  - 反向信号。
  - 可选止损、止盈或移动止损。
- **止损**：支持，自定义。
- **默认值**：
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **过滤器**：
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: 多个
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
