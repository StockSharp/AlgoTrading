# Two MA RSI 策略

## 概述
Two MA RSI 策略来自 MetaTrader 专家顾问“2MA_RSI”的移植版本。策略结合了一条快 EMA 和一条慢 EMA 的金叉/死叉，并用 RSI 过滤信号。下单量采用类似马丁格尔的资金管理：每次亏损后会扩大下一笔订单的数量。StockSharp 版本仅在每根 K 线收盘后运行，并按照点数重新计算原策略中的止盈与止损。

## 数据与指标
- 只订阅一个由 `CandleType` 指定的蜡烛序列（默认 5 分钟）。
- 每根完成的 K 线都会更新三个指标：
  - `FastLength` 长度的 EMA（使用收盘价）。
  - `SlowLength` 长度的 EMA。
  - `RsiLength` 长度的 RSI。
- 策略内部保存上一根 K 线的 EMA 值，用于检测金叉/死叉，无需访问指标缓冲区。

## 入场逻辑
1. 必须在上一根 K 线收盘后评估信号，避免盘中重复触发。
2. 当前必须没有持仓（`Position == 0`）。
3. **做多条件：**
   - 快 EMA 从下往上穿越慢 EMA（当前快 EMA > 慢 EMA，上一根快 EMA < 慢 EMA）。
   - RSI 低于 `RsiOversold`，显示市场超卖。
4. **做空条件：**
   - 快 EMA 从上往下穿越慢 EMA（当前快 EMA < 慢 EMA，上一根快 EMA > 慢 EMA）。
   - RSI 高于 `RsiOverbought`，显示市场超买。
5. 满足条件时发送市价单，数量由马丁格尔模块决定。

## 出场逻辑
- 入场后立即根据点数计算止损和止盈，点数会乘以标的的 `PriceStep` 转成价格：
  - **多头：**
    - 止损 = `入场价 - StopLossPoints * PriceStep`。
    - 止盈 = `入场价 + TakeProfitPoints * PriceStep`。
  - **空头：**
    - 止损 = `入场价 + StopLossPoints * PriceStep`。
    - 止盈 = `入场价 - TakeProfitPoints * PriceStep`。
- 只有触发这些保护价位才会平仓。策略在下一根 K 线检查最高价和最低价是否触碰目标，并调用 `ClosePosition()` 发出市价离场。
- 如果同一根 K 线同时覆盖止盈和止损区间，会优先判定止损，保持与原有 EA 相同的保守行为。

## 仓位管理与马丁格尔
1. 每次入场前计算基础下单量：`floor(balance / BalanceDivider) * VolumeStep`。余额优先使用投资组合的 `CurrentValue`，若不可用则使用 `BeginValue`，并确保不低于一个成交量步长。
2. 每次亏损后马丁格尔阶段加一，但不超过 `MaxDoublings`，下一次下单量乘以 `2^stage`。
3. 任意盈利或达到最大加倍次数都会将阶段归零，恢复基础下单量。
4. 当 `MaxDoublings` 小于或等于零时，策略不会放大仓位，始终采用基础下单量。

## 其他行为
- 策略内部保存所需的 EMA 值，不使用额外的数据结构。
- 只有在指标已形成且允许交易 (`IsFormedAndOnlineAndAllowTrading`) 时才会发送订单。
- 图表会绘制价格 K 线、自己的成交记录以及三个指标曲线，便于可视化分析。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `FastLength` | 快速 EMA 的周期。 | 5 |
| `SlowLength` | 慢速 EMA 的周期。 | 20 |
| `RsiLength` | RSI 的计算周期。 | 14 |
| `RsiOverbought` | 允许做空的 RSI 超买阈值。 | 70 |
| `RsiOversold` | 允许做多的 RSI 超卖阈值。 | 30 |
| `StopLossPoints` | 以价格步长表示的止损距离。 | 500 |
| `TakeProfitPoints` | 以价格步长表示的止盈距离。 | 1500 |
| `BalanceDivider` | 将账户价值除以该系数得到基础下单量。 | 1000 |
| `MaxDoublings` | 连续亏损后允许的最大加倍次数。 | 1 |
| `CandleType` | 使用的蜡烛类型。 | 5 分钟 |

## 使用提示
- 请确保证券的 `PriceStep` 和 `VolumeStep` 已设置，否则点数和下单量无法正确换算。
- 由于采用市价平仓，实际成交仍可能出现滑点，但止损/止盈的逻辑与原 EA 保持一致。
- 本次仅提供 C# 实现，未创建 Python 版本或对应目录。
