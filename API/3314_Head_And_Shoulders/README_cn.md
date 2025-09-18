# 头肩形策略

## 概述
**头肩形策略**直接移植自 MetaTrader 专家顾问“HEAD AND SHOULDERS”（MQL ID 26066）。原始 EA 通过识别头肩形形态，并结合动量、均线与 MACD 过滤条件，同时提供移动止损、权益保护和保本处理。本版本在 StockSharp 高级 API 上重写交易逻辑，使用指标绑定和 `StartProtection` 自动化风险控制，保留核心的形态识别与突破入场思想。

## 交易逻辑
1. **形态识别**
   - 使用 5 根 K 线窗口构建分形高点与低点，模拟原始 EA 的分形探测算法。
   - 当连续三个分形高点出现且中间高点（头部）高于两侧肩部并满足主导百分比条件时，确认出现看空头肩形。
   - 当连续三个分形低点出现且中间低点明显低于两侧肩部时，确认出现倒头肩形（看多形态）。
   - 颈线价格由肩部与头部之间最近的分形低点（看空）或分形高点（看多）平均得到。
2. **动量与趋势过滤**
   - 快速与慢速简单移动平均线必须顺应预期的趋势方向。
   - 动量指标的绝对值需超过阈值，且方向与入场方向一致。
   - MACD 的数值必须支持突破方向，防止逆势信号。
3. **突破执行**
   - 当收盘价上破倒头肩形颈线且全部过滤条件成立时开多。
   - 当收盘价跌破头肩形颈线且过滤条件同样满足时开空。
4. **仓位管理**
   - 如果价格反向穿越颈线或均线、MACD 失去趋势一致性，则平仓离场。
   - 止损、止盈与移动止损通过 `StartProtection` 按价格步长设定，可根据需要启用或关闭。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 1 小时周期 | 形态识别使用的主时间框架。 |
| `OrderVolume` | `1` | 基础下单手数。 |
| `FastMaLength` / `SlowMaLength` | `6` / `85` | 趋势过滤使用的快慢均线长度。 |
| `MomentumPeriod` | `14` | 动量指标回溯周期。 |
| `MomentumThreshold` | `0.3` | 动量绝对值的最小确认阈值。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `12`, `26`, `9` | MACD 参数设置。 |
| `ShoulderTolerancePercent` | `5` | 左右肩部允许的最大高度偏差（百分比）。 |
| `HeadDominancePercent` | `2` | 头部必须高/低于肩部的最小百分比。 |
| `StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps` | `100`, `200`, `0` | 保护单所用的价格步长，0 表示禁用。 |

所有参数均通过 `Param()` 创建，包含显示元数据，可在 StockSharp 优化器中直接进行参数优化。

## 与原始 EA 的差异
- 去除了 MetaTrader 平台特有的权益止损、复杂移动止损与订单修改流程，改用 StockSharp 的内建保护机制。
- 仅使用高层 API 的市价单（`BuyMarket` / `SellMarket`），不再直接操作订单票据。
- 不再绘制图形对象、发送通知，而是使用 `LogInfo` 输出信号日志。
- 分形模式识别逻辑按高层 API 规范重写，但仍保留“头肩形 + 分形确认”的核心思路。

## 使用提示
- 策略只在 K 线完成 (`CandleStates.Finished`) 后运算，请确保数据订阅提供完整收盘柱。
- 移动保护参数以价格步长表示，启用前请确认 `Security.PriceStep` 与交易品种的最小跳动单位一致。
- 为避免无限增长的缓存，策略仅保存最近的分形点，适合长时间运行。
- 若需要原 EA 中的多时间框架确认，可按相同绑定方式新增更高周期的订阅和指标。

## 参考
- MetaTrader 专家顾问：`HEAD AND SHOULDERS.mq4`（MQL ID 26066）。
- StockSharp 文档：[高级策略 API](https://doc.stocksharp.com/topics/strategy/highlevel.html)、[指标绑定](https://doc.stocksharp.com/topics/strategy/highlevel/bind.html)。
