# Snowieso 策略

该策略结合快速和慢速的 **线性加权移动平均线 (LWMA)**、**MACD** 以及 **Kaufman 自适应移动平均线 (KAMA)** 来确认趋势方向。

## 工作原理
1. 订阅所选时间框架的K线。
2. 计算 Fast LWMA、Slow LWMA、MACD 和 KAMA 的数值。
3. **做多**：当快速 LWMA 向上穿越慢速 LWMA、MACD 柱状图为正且 KAMA 上升时触发。
4. **做空**：当快速 LWMA 向下穿越慢速 LWMA、MACD 柱状图为负且 KAMA 下降时触发。
5. 通过 `StartProtection` 设置固定的止损和止盈。

策略在开新仓前会先平掉相反方向的仓位，并在图表上展示指标和交易。

## 参数
- `FastLength` – 快速 LWMA 的周期。
- `SlowLength` – 慢速 LWMA 的周期。
- `MacdFast`、`MacdSlow`、`MacdSignal` – MACD 参数设置。
- `KamaLength` – KAMA 的计算周期。
- `StopLossPoints` – 以价格点表示的固定止损。
- `TakeProfitPoints` – 以价格点表示的固定止盈。
- `CandleType` – 处理的K线时间框架。

## 使用方法
在选定的证券上运行该策略。算法会自动订阅K线并根据指标信号管理仓位。使用高级 API 进行数据绑定和订单执行。
