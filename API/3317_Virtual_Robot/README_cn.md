# Virtual Robot 策略

## 概述

Virtual Robot 策略重现了原始 MetaTrader EA 的虚拟网格平均法。策略在指定的蜡烛周期上维护多头与空头两组虚拟挂单。当某一侧的虚拟挂单数量达到阈值后才会发送真实市价单，从而沿用 MT4 中“虚拟引导真实” 的思路。

## 交易流程

1. **构建虚拟网格**：
   - 收盘价高于开盘价时，只要与上一虚拟多头层的距离超过 `PipStepPips`，就新增一个多头虚拟层；
   - 收盘价低于开盘价时，按同样规则新增空头虚拟层；
   - 前 `VirtualStepper` 个虚拟层使用基础手数，此后按 `Multiplier` 放大。
2. **触发真实下单**：
   - 当虚拟层数 ≥ `StartingRealOrders`（或已有仓位回撤超过一个步长）时，按照 `Multiplier * (回撤 / PipStepPips)` 计算下一笔真实手数并发送市价单；
3. **篮子管理**：
   - 记录每个方向最近一次成交的价格与手数；
   - 根据 `RealAverageThreshold` 决定使用虚拟均价还是真实均价来跟踪篮子成本。
4. **止盈条件**：
   - 价格自首个虚拟层起盈利 `MinTakeProfitPips`；
   - 价格回到虚拟加权均价 ± `AverageTakeProfitPips`；
   - 或触发由 `TakeProfitPips`/`AverageTakeProfitPips` 计算出的单单/均价止盈。
5. **止损条件**：
   - 以最新真实订单为基准，根据 `StopLossPips` 计算软性止损，只要价格触及便平掉整篮仓位。
6. **手数校验**：
   - 所有手数都会根据证券的 `VolumeStep`、`MinVolume`、`MaxVolume` 校准，并受 `MaxVolume` 限制。

## 参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 构建虚拟网格所用的蜡烛类型（默认 60 分钟）。 |
| `StopLossPips` | 距离最近成交价的止损点数。 |
| `TakeProfitPips` | 单笔篮子的止盈点数。 |
| `MinTakeProfitPips` | 关闭首个虚拟层所需的最小盈利。 |
| `AverageTakeProfitPips` | 多层网格回归均价后的盈利目标。 |
| `BaseVolume` | 第一批订单的基础手数。 |
| `MaxVolume` | 允许的最大手数。 |
| `Multiplier` | 加仓时的手数放大系数。 |
| `RealStepper` | 累计多少真实订单后开始使用放大系数。 |
| `VirtualStepper` | 多少虚拟订单使用基础手数。 |
| `PipStepPips` | 相邻网格层之间的最小回撤（点）。 |
| `MaxTrades` | 每个方向最多允许的真实订单数量。 |
| `StartingRealOrders` | 触发第一笔真实订单所需的虚拟层数。 |
| `RealAverageThreshold` | 达到该真实订单数量后改用真实均价。 |
| `VisualMode` | 为兼容 MT4 输入保留，在 StockSharp 中不生效。 |

## 实现细节

- StockSharp 采用净持仓模型，因此无法像 MT4 对冲模式那样同时持有独立的多/空篮子；若两侧同时触发，后来的信号会把净持仓翻向对应方向。
- 原 EA 的图形对象未实现，所有虚拟层仅保存在内存中。
- 点值从 `Security.PriceStep` 计算，对三位和五位报价货币对额外乘以 10，以匹配 MT4 的 pip 换算。
- 止损与止盈通过代码检测价格并发送市价平仓，而不是挂出真正的止损/限价单。

## 使用建议

1. 确认证券信息中已填好 `PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume`，以避免手数或点值换算偏差。
2. 建议先在仿真或小手数环境测试，确认网格间距与倍数符合预期。
3. 通过调整 `StartingRealOrders` 与 `RealStepper` 控制加仓节奏和风险敞口。
