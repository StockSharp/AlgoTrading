# Smart Ass Trade 策略

Smart Ass Trade 是从 MQL 版本转换而来的多时间框趋势跟随策略。
它在 5、15、30 分钟图上计算 MACD 直方图（OsMA）和 20 周期简单移动平均线，
并使用日线 Williams %R 过滤过度买入或卖出。

## 算法
1. 在 5m、15m、30m 时间框架上计算 MACD 直方图和 SMA(20)。
2. 当所有时间框架直方图上升且 SMA 上行时判断为上升趋势。
3. 当所有时间框架直方图下降且 SMA 下行时判断为下降趋势。
4. 使用日线 Williams %R（周期 26）避免在 -2 以上买入或在 -98 以下卖出。
5. 满足条件时按方向开市价单。
6. 头寸大小可固定或根据账户资金自动调整。

## 参数
- **Hedging** – 允许同时持有相反仓位。
- **LotsOptimization** – 启用动态手数计算。
- **Lots** – 关闭优化时的固定交易量。
- **AutomaticTakeProfit** – 动态止盈占位符，目前未实现。
- **MinimumTakeProfit** – 手动模式下的止盈点数。
- **AutomaticStopLoss** – 动态止损占位符，目前未实现。
- **StopLoss** – 手动模式下的止损点数。
- **CandleType** – 订阅的基础时间框架（默认 5 分钟）。

## 说明
该策略使用高级 API，通过 `SubscribeCandles` 和 `Bind` 获取指标数据。
当前版本主要关注信号生成与下单，止盈止损参数保留以供后续扩展。
