# T3MA(MTC) 策略

本策略来自 MetaTrader 4 智能交易系统 **T3MA(MTC).mq4**（目录 `MQL/7904`）。原始 EA 依赖 “T3MA-ALARM” 指标：先对行情做双重指数平滑，当平滑曲线的斜率从下行变为上行或从上行转为下行时就开仓。移植到 StockSharp 后，算法保持不变，并使用官方的高层 API 实现。

## 交易思路

1. 根据所选蜡烛类型与周期计算第一条 EMA。
2. 再用相同周期的第二条 EMA 对结果进行二次平滑。
3. 将当前平滑值与前一值比较（若 `MaShift` 大于 0，会先进行相应的位移）。
4. 当斜率方向发生切换时生成信号。订单会在 `CalculationBarOffset` 根已完成蜡烛之后执行，对应原始 EA 的 `CalculationBarIndex` 设置。
5. 每个信号都会记录该蜡烛的最低价（做多）或最高价（做空）作为唯一标记，避免重复下单，这就是 MQL 中 `LastOrder` 变量的等价物。

## 移植要点

- 通过两个 `ExponentialMovingAverage` 实例复刻 T3MA-ALARM 的双重平滑链条。
- 使用一个小型队列保存最近的平滑值，以便正确应用 `MaShift`。
- 将信号存入 FIFO 队列，只有在等待了指定数量的已完成蜡烛后才会真正执行。
- 借助 `StartProtection` 设置止盈止损，距离单位为价格步长，与 MetaTrader 中的 “Point” 完全一致。
- `AllowMultiplePositions` 标志对应 `MultiPositions` 参数：关闭后，策略会等待净头寸归零才会处理下一次信号。

## 参数说明

- `MaPeriod` – 两次 EMA 平滑所使用的周期（默认 4）。
- `MaShift` – 比较斜率前对平滑值做的位移量（默认 0）。
- `CalculationBarOffset` – 信号延迟执行的已完成蜡烛数量（默认 1）。
- `TradeVolume` – 下单手数（默认 1）。
- `UseStopLoss` / `StopLossPoints` – 是否启用止损及其价格步长距离（默认启用，40 步）。
- `UseTakeProfit` / `TakeProfitPoints` – 是否启用止盈及其价格步长距离（默认启用，11 步）。
- `AllowMultiplePositions` – 是否允许在已有反向持仓时继续加仓（默认启用）。
- `CandleType` – 参与计算的蜡烛类型或时间框架（默认 5 分钟）。

## 执行流程

1. 订阅蜡烛数据，并将收盘价送入双重 EMA 链。
2. 跟踪当前斜率方向，发现翻转时生成信号。
3. 将信号（或空信号）压入延迟队列，确保执行时间精确地推迟 `CalculationBarOffset` 根已完成蜡烛，与原版 EA 读取较旧指标缓冲区的方式一致。
4. 信号出队准备执行时：
   - 如果交易未被允许、平台未就绪，或在 `AllowMultiplePositions` 关闭且仍有持仓的情况下，则跳过。
   - 确认价格标记与上一笔不同，防止重复下单。
   - 依据方向调用 `BuyMarket`/`SellMarket` 以设定手数市价开仓，并自动附加启用的止盈止损。

## 其它说明

- 比较价格时会加入微小容差，避免在模拟 `LastOrder` 时出现浮点误差。
- 当 `AllowMultiplePositions` 关闭时，策略不会主动反手，完全遵循原始 EA 依赖保护性止损/止盈退出的设计。
- 如果环境支持图形模块，可显示蜡烛图与自身成交情况，方便调试。
