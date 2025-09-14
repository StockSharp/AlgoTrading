# Forex Fraus 4 For M1s 策略
[English](README.md) | [Русский](README_ru.md)

转换自 MQL4 策略 #13643。原始 EA 在 Williams %R 指标触及极值并反转时进场。此 C# 版本基于 StockSharp 的高级 API。

策略使用 1 分钟 K 线，并关注两个关键水平：
- 当 Williams %R 从低于 -99.9 上穿该值时触发做多信号。
- 当 Williams %R 从高于 -0.1 下穿该值时触发做空信号。

仓位可通过固定止损、止盈或跟踪止损退出。时间过滤器允许限制交易在特定时段内进行。

## 细节

- **入场条件**  
  - 多头：`WilliamsR` 从下方上穿 `BuyThreshold` (-99.9)。  
  - 空头：`WilliamsR` 从上方下穿 `SellThreshold` (-0.1)。
- **多空方向**：双向
- **出场条件**  
  - 价格达到止损 (`StopLoss`) 或止盈 (`TakeProfit`)  
  - 启用时的跟踪止损 (`TrailingStop`)
- **止损类型**：以价格步长计算
- **默认参数**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选标签**  
  - 分类：趋势反转  
  - 方向：双向  
  - 指标：Williams %R  
  - 止损：有  
  - 复杂度：基础  
  - 时间框架：日内 (M1)  
  - 季节性：无  
  - 神经网络：无  
  - 背离：无  
  - 风险等级：中等

