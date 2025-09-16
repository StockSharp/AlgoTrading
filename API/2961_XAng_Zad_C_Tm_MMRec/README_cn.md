# XAng Zad C TM MM Rec 策略

## 概览
该策略是 MetaTrader 智能交易系统 **Exp_XAng_Zad_C_Tm_MMRec** 的 C# 移植版本。它基于自定义的 *XAng Zad C* 自适应通道，结合日内交易时间窗口与仓位递减式风控模块，用于在上下通道交叉时捕捉突破行情，并在出现连续亏损时自动减小下单手数。

### 核心逻辑
- **指标**：XAng Zad C 指标生成自适应上轨和下轨。C# 实现复刻了包络线算法，并支持 SMA、EMA、SMMA、LWMA 等平滑方式；原脚本中无法直接对应的平滑方式会自动退化为 EMA。
- **入场条件**：当上一根 K 线的上轨高于下轨，当前 K 线收盘后上轨跌破下轨时触发做多信号；反向情形触发做空信号。`SignalShift` 参数决定比较的历史 K 线数量。
- **离场条件**：可选的布尔参数允许在上轨重新跌破下轨时平掉多单，在下轨重新突破上轨时平掉空单；一旦超出交易时间窗口，也会立即平仓。
- **资金管理**：策略维护一份交易结果列表。如果最近 `BuyTotalTrigger`（或 `SellTotalTrigger`） 笔交易中出现了不少于 `BuyLossTrigger`（或 `SellLossTrigger`） 次亏损，下次下单会使用减小后的手数，否则恢复默认手数。
- **风控**：止损与止盈均以价格最小变动单位（`Security.PriceStep`）为倍数设置，一旦在 K 线范围内触达即按照对应价格平仓。

## 参数说明
| 名称 | 描述 |
| --- | --- |
| `NormalVolume` | 正常情况下使用的下单数量。 |
| `ReducedVolume` | 连续亏损后启用的缩减数量。 |
| `BuyTotalTrigger` / `SellTotalTrigger` | 资金管理回溯的历史交易数量。 |
| `BuyLossTrigger` / `SellLossTrigger` | 触发缩减仓位所需的亏损次数。 |
| `EnableBuyEntries` / `EnableSellEntries` | 是否允许做多 / 做空开仓。 |
| `EnableBuyExit` / `EnableSellExit` | 是否允许根据通道交叉自动平仓。 |
| `UseTradingWindow` | 是否启用日内交易时间过滤。 |
| `WindowStart` / `WindowEnd` | 每日交易窗口的起止时间（UTC，可跨越午夜）。 |
| `StopLoss` | 以 `PriceStep` 为单位的固定止损距离，0 表示禁用。 |
| `TakeProfit` | 以 `PriceStep` 为单位的固定止盈距离，0 表示禁用。 |
| `SignalShift` | 参与交叉比较的历史 K 线数。 |
| `CandleType` | 指标所使用的 K 线类型（默认 4 小时）。 |
| `SmoothMethod` | 指标内部的平滑方式，不支持的选项自动退化为 EMA。 |
| `MaLength` | 平滑长度。 |
| `MaPhase` | 保留的相位参数（当前仅用于兼容）。 |
| `Ki` | 控制包络线响应速度的比例系数。 |
| `AppliedPrice` | 指标使用的价格类型（收盘价、开盘价、中价等）。 |

## 与 MQL5 版本的区别
- 原版资金管理依赖全局历史订单。C# 版本通过本地记录完成的交易来复刻同样的触发逻辑。
- 下单数量直接对应策略的交易量，需根据交易所或经纪商设置 `NormalVolume` 和 `ReducedVolume`。
- 交易时间窗口使用 `TimeSpan` 表示；当起止时间完全相同时视为禁用，与原脚本的零长度窗口行为一致。
- 策略假设每次信号都会完全平掉上一笔仓位，不支持保留部分旧仓。
- 对于没有现成实现的平滑方式（如 JJMA、JurX、ParMA、T3、VIDYA、AMA）会自动回退为 EMA，如需特定算法可扩展 `CreateMovingAverage`。

## 使用建议
1. 选择与原始 EA 相同的指标时间框（默认 H4）。
2. 根据标的品种的最小跳动点值调整止损/止盈距离，使其接近原脚本的点数设定。
3. 结合标的波动性与风险承受度优化资金管理触发参数。
4. 建议在图表上同时绘制上下通道，确认 C# 指标输出与原版一致后再投入实盘或回测。
