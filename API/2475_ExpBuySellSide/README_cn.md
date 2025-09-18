# ExpBuySellSide策略
[English](README.md) | [Русский](README_ru.md)

该策略是将MetaTrader专家顾问**ExpBuySellSide**转换到StockSharp API的版本。它结合了基于ATR的止损系统和简化的Step Up/Down趋势过滤器。

ATR模块在每根K线上构建动态带。当价格突破上轨时视为进入多头阶段；跌破下轨则视为进入空头阶段。

Step Up/Down模块比较快速SMA与慢速SMA，并检查它们之间的差值是否在扩大。差值沿交叉方向扩大说明趋势得到确认。

只有当**两个**模块同时给出同向信号时才开仓。若启用“Close Opposite”，出现反向信号时会先平掉已有仓位。

## 细节

- **入场条件**：
  - **做多**：收盘价突破ATR上轨且快速SMA相对慢速SMA上升。
  - **做空**：收盘价跌破ATR下轨且快速SMA相对慢速SMA下降。
- **方向**：双向。
- **出场条件**：
  - 出现反向信号并启用*Close Opposite*。
  - 通过保护函数手动平仓。
- **止损**：基于`ATR * Multiplier`。
- **默认参数**：
  - `ATR Period` = 5。
  - `ATR Multiplier` = 2.5。
  - `Fast SMA` = 2。
  - `Slow SMA` = 30。
  - `Candle Type` = 1小时。
  - `Close Opposite` = true。
- **过滤标签**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

