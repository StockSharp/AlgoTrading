# VininI Trend 策略

## 描述
该策略将 MQL5 顾问 **Exp_VininI_Trend** 转换为 StockSharp 实现。使用商品通道指数 (CCI) 模拟 VininI Trend 振荡器。当振荡器突破上轨或向上转折时开多仓；当振荡器跌破下轨或向下转折时开空仓。策略仅在蜡烛收盘后处理信号。

## 参数
- **CCI Period** – CCI 指标的周期。
- **Upper Level** – 触发买入信号的上限。
- **Lower Level** – 触发卖出信号的下限。
- **Entry Mode** – `Breakdown` 处理突破，`Twist` 处理方向变化。
- **Candle Type** – 计算所用蜡烛的时间框架。

## 原始来源
基于 `MQL/1365/exp_vinini_trend.mq5` 的 MQL5 策略改写。
