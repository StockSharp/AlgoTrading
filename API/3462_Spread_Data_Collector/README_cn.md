# 点差统计采集策略

## 概述
**Spread Data Collector 策略** 是 MetaTrader 5 工具“Spread data collector”（MQL 编号 33314）的 StockSharp 版本。原始 EA 不会下单，只是监听买/卖报价并统计点差落在各个范围内的次数；当交易年度发生变化或 EA 被停止时，它会打印汇总结果。本移植使用高级的 `SubscribeLevel1()` API，实现完全相同的逻辑，并允许用户修改每个点差区间的上限。

## 运行流程
- 启动时，策略会订阅主交易品种 `Security` 的一级行情（买/卖价）。
- 只要同时得到买价和卖价，就会计算点差，并通过 `Security.PriceStep` 将“点”转换为实际价格差。
- 策略维护六个计数器：
  1. 点差严格小于第一条上限。
  2. 点差位于第一与第二条上限之间。
  3. 点差位于第二与第三条上限之间。
  4. 点差位于第三与第四条上限之间。
  5. 点差位于第四与第五条上限之间。
  6. 点差大于或等于第五条上限。
- 交易年度来自 `Level1ChangeMessage.ServerTime` 的交易所时间。当年份发生变化时，策略会输出上一年的统计并清零计数。
- 停止策略时，会先输出当年的统计结果，再结束运行。

因此，该策略与原工具一样完全“被动”，适合长期监控流动性与点差质量，不会发送任何订单。

## 参数
所有参数以 **点 (points)** 表示，实际价格差通过公式 `points × Security.PriceStep` 计算。

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FirstBucketPoints` | 10 | 第一个区间的上限，点差严格小于该值时计入此组。 |
| `SecondBucketPoints` | 20 | 第二个区间的上限，点差落在 `[FirstBucketPoints, SecondBucketPoints)` 内计入此组。 |
| `ThirdBucketPoints` | 30 | 第三个区间的上限，点差落在 `[SecondBucketPoints, ThirdBucketPoints)` 内计入此组。 |
| `FourthBucketPoints` | 40 | 第四个区间的上限，点差落在 `[ThirdBucketPoints, FourthBucketPoints)` 内计入此组。 |
| `FifthBucketPoints` | 50 | 第五个区间的上限，点差落在 `[FourthBucketPoints, FifthBucketPoints)` 内计入此组。 |

所有阈值必须严格递增。如果 `Security.PriceStep` 未设置或小于等于 0，策略在启动时会抛出异常，以避免产生错误的统计数据。

## 日志输出
策略通过 `AddInfoLog` 打印统计信息，格式如下：

```
Year=2024 Spread<=10pts=15342 Spread_10_20pts=2841 Spread_20_30pts=912 ... Spread>50pts=37
```

该格式与 MetaTrader 中的 `Print` 输出相同，方便在不同平台之间对比。可以使用 StockSharp 日志查看器或将日志重定向到文件进行分析。

## 使用步骤
1. 在 `Strategy.Security` 中指定品种，并确保 `PriceStep` 与 MetaTrader 中“点”的定义一致（多数外汇品种为 0.0001）。
2. 如需不同的点差区间，可调整各个上限，务必保持严格递增。
3. 启动策略即可，无需担心订单会被发送。
4. 定期查看年度日志，了解点差在不同时间段的表现。

该策略资源占用极低，可以与实盘交易系统并行运行，用于构建长期的点差分布数据、验证流动性假设以及监控券商条件。
