# Secwenta 多重K线信号策略

## 概述
该策略是 MetaTrader 专家顾问 "Secwenta"（MQL 编号 22977）的 StockSharp 移植版本。算法会读取指定周期内已完成的 K 线，统计其中收盘价高于开盘价的阳线数量和收盘价低于开盘价的阴线数量。根据参数设置，策略可以仅做多、仅做空，或在多空之间自动反手。当累计的阳线或阴线数量达到阈值时，策略会按照原版 EA 的固定手数开仓或平仓。

## 信号评估
- 通过高层订阅接口，仅处理所选 `CandleType` 的已完成 K 线。
- 每根 K 线都会被标记为阳线、阴线或中性（十字星）。内部缓冲区保存最近 *N* 根 K 线的方向，其中 *N* 取决于激活方向对应的 `BullishBarCount` 与 `BearishBarCount` 的较大值。
- 阳线计数器在 K 线收盘价高于开盘价时递增，阴线计数器在收盘价低于开盘价时递增。中性 K 线不会改变计数。
- 当任一计数器在滑动窗口内达到设定阈值时，就会触发信号。这与原始 MQL 代码依次遍历最近几根 K 线、直到出现足够多的阳线或阴线的流程完全一致。

## 交易规则
1. **仅做多模式（`UseBuySignals = true`，`UseSellSignals = false`）**
   - 当阴线计数达到 `BearishBarCount` 时，若当前持有多单，则以市价卖出平仓。
   - 当阳线计数达到 `BullishBarCount` 且当前为空仓时，以 `OrderVolume` 的手数买入建仓。
2. **仅做空模式（`UseBuySignals = false`，`UseSellSignals = true`）**
   - 当阳线计数达到 `BullishBarCount` 时，若当前持有空单，则以市价买入平仓。
   - 当阴线计数达到 `BearishBarCount` 且当前为空仓时，以 `OrderVolume` 的手数卖出建仓。
3. **双向反手模式（`UseBuySignals = true` 且 `UseSellSignals = true`）**
   - 触发阳线信号时，首先买入平掉现有空单；若策略尚未持有多单，则再额外买入 `OrderVolume` 手，实现从空头到多头的切换。
   - 触发阴线信号时，先卖出平掉现有多单；若策略尚未持有空单，则再卖出 `OrderVolume` 手，完成多头到空头的转换。

所有下单都使用 StockSharp 的 `BuyMarket` 与 `SellMarket` 辅助方法，并调用 `StartProtection()`，方便与平台提供的账户保护机制配合使用。

## 参数
| 参数 | 说明 | 默认值 | 备注 |
|------|------|--------|------|
| `CandleType` | 用于统计序列的 K 线类型（时间框架）。 | 1 小时 K 线 | 可更换为任意受支持的蜡烛或时间框架。 |
| `OrderVolume` | 下单基准手数，对应原 EA 的 Lots 设置。 | 1 | 在反手时会自动叠加到平仓数量中。 |
| `UseBuySignals` | 是否启用看涨信号。 | `true` | 关闭后不会再开立新的多单。 |
| `BullishBarCount` | 触发看涨事件所需的阳线数量。 | 2 | 在仅做多模式下，应与平仓阈值保持协调。 |
| `UseSellSignals` | 是否启用看跌信号。 | `false` | 关闭后不会再开立新的空单。 |
| `BearishBarCount` | 触发看跌事件所需的阴线数量。 | 1 | 既用于开空，也用于多单的平仓阈值。 |

## 实现要点
- 使用队列维护最新的 K 线方向，并在参数调整时同步更新计数，保证窗口长度正确。
- 仅处理状态为 `Finished` 的 K 线，保持与原 MQL 在新柱开始时进行计算的行为一致。
- 中性（十字星）K 线不会影响计数，与原始实现相同。
- 在需要反手时，单笔市价单同时完成平仓和开仓，确保持仓变化具有确定性。
- 缓冲区长度只由已启用方向的阈值决定；若某一方向被禁用，则只会统计另一方向的阈值，与原版 `CopyRates` 的使用方式保持一致。

## 文件
- `CS/SecwentaMultiBarSignalsStrategy.cs` – 基于 StockSharp 高层策略 API 的 C# 实现。

> **说明：** 按需求仅提供 C# 版本，本目录中没有 Python 代码或 PY 子目录。
