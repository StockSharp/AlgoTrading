# 平均蜡烛交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略重现了 MetaTrader 上的 “Average candle cross” 专家顾问。它只在上一根已完成的蜡烛向上或向下穿越均线、并且两组趋势均线已经指向同一方向时开仓。系统始终只持有一笔仓位，入场后立即根据设定的点值距离放置止损和止盈，使行为与原始的逐棒触发逻辑一致。

所有计算都基于已完成的 K 线，避免了未收盘行情带来的噪声。多头与空头拥有独立的均线参数，可以实现不同的平滑方式或周期。止损与止盈使用真实的挂单，距离等于 `StopLossPips * PipSize`，而止盈距离则为该值乘以对应的百分比参数。

## 细节

- **入场条件**：
  - **做多**：上一根 K 线的两条趋势均线都向上 (`MA_fast1[1] > MA_slow1[1]` 且 `MA_fast2[1] > MA_slow2[1]`)，上一根 K 线收盘价高于交叉均线，同时前一根 K 线收盘价位于均线之下 (`Close[2] <= MA_cross[2]` 且 `Close[1] > MA_cross[1]`)。
  - **做空**：上一根 K 线的两条趋势均线都向下 (`MA_fast1[1] < MA_slow1[1]` 且 `MA_fast2[1] < MA_slow2[1]`)，上一根 K 线收盘价低于交叉均线，同时前一根 K 线收盘价位于均线之上 (`Close[2] >= MA_cross[2]` 且 `Close[1] < MA_cross[1]`)。
- **交易方向**：多空双向，但不会同时持仓。
- **离场方式**：
  - 仅通过止损或止盈单离场。
- **止损/止盈**：是。止损距离为 `StopLossPips * PipSize`，止盈距离为止损距离乘以 “% of SL” 参数。
- **默认参数**：
  - `FirstTrendFastPeriod` = 5，`FirstTrendFastMethod` = SMA。
  - `FirstTrendSlowPeriod` = 20，`FirstTrendSlowMethod` = SMA。
  - `SecondTrendFastPeriod` = 20，`SecondTrendFastMethod` = SMA。
  - `SecondTrendSlowPeriod` = 30，`SecondTrendSlowMethod` = SMA。
  - `BullCrossPeriod` = 5，`BullCrossMethod` = SMA。
  - `BuyVolume` = 0.01，`BuyStopLossPips` = 50，`BuyTakeProfitPercent` = 100。
  - `FirstTrendBearFastPeriod` = 5，`FirstTrendBearFastMethod` = SMA。
  - `FirstTrendBearSlowPeriod` = 20，`FirstTrendBearSlowMethod` = SMA。
  - `SecondTrendBearFastPeriod` = 20，`SecondTrendBearFastMethod` = SMA。
  - `SecondTrendBearSlowPeriod` = 30，`SecondTrendBearSlowMethod` = SMA。
  - `BearCrossPeriod` = 5，`BearCrossMethod` = SMA。
  - `SellVolume` = 0.01，`SellStopLossPips` = 50，`SellTakeProfitPercent` = 100。
  - `PipSize` = 0.0001。
- **过滤维度**：
  - 类型：趋势跟随。
  - 方向：双向（多头与空头）。
  - 指标：多条移动平均线。
  - 止损：固定点数止损，按比例止盈。
  - 复杂度：中等。
  - 周期：使用配置的蜡烛序列（默认 15 分钟）。
  - 季节性：否。
  - 神经网络：否。
  - 背离：否。
  - 风险水平：中等。
