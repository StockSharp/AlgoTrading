# MACD 信号策略

## 概述
**MACD 信号策略** 是 MetaTrader 专家顾问 `MACD_signal.mq4` 的 StockSharp 版本。原始脚本通过比较 MACD 直方图与基于
ATR 的波动通道，只要直方图越过该通道就开出一笔市价单。此 C# 实现使用 StockSharp 的高级 API 重现同样的动量
突破逻辑，显式保存上一根柱子的直方图和 ATR 读数，并在源码中以英文注释清晰说明每一个资金管理规则。

与直接修改单据的 MetaTrader 版本不同，StockSharp 运行在净头寸模式下。因此在翻转方向之前必须先平掉已有
的持仓，并在策略内部维护跟踪止损，而不是依赖经纪商侧的 `OrderModify` 调整。

## 交易逻辑
1. 订阅参数 `CandleType` 指定的 K 线序列，只处理已经收盘的蜡烛，避免半成品数据造成噪音。
2. 使用设定的快线、慢线和信号 EMA 周期驱动 `MovingAverageConvergenceDivergenceSignal` 指标，并在每根 K 线收
   盘时记录一次直方图数值（`MACD - signal`）。
3. 在同一套 K 线上计算 `AverageTrueRange`。上一根 K 线的 ATR 乘以 `ThresholdMultiplier`，复刻 MQL 版本中
   `rr = ATR * LEVEL` 的阈值。
4. 当当前直方图突破 `+threshold` 且上一根柱子仍低于该值时视为多头突破。如果当前为空仓或空头并且 `Direction`
   允许做多，则按 `TradeVolume` 下达市价买单。
5. 当当前直方图跌破 `-threshold` 且上一根柱子仍高于该值时视为空头突破。如果当前为空仓或多头并且允许做
   空，则按 `TradeVolume` 下达市价卖单。
6. 每根 K 线都会检查持仓：
   - 一旦直方图变成负值就平掉多单，变成正值就平掉空单；
   - 将 `TakeProfitPoints` 转换为价格步长，对比蜡烛最高价/最低价以模拟原始 EA 的止盈设置；
   - 当盈利幅度超过 `TrailingStopPoints` 后启动跟踪止损，若价格回落到跟踪价位立即离场。多单的跟踪价以收
     盘价近似买价，空单则以收盘价近似卖价。
7. 如果 `TakeProfitPoints` 小于历史上设定的 10 点下限，策略会拒绝交易，与 MQL 中的防护逻辑完全一致。

## 风险控制
- **同一时间仅有一笔订单。** 策略在开新仓之前会先将净头寸归零，复刻 `OrdersTotal() < 1` 的约束。
- **固定仓位。** `TradeVolume` 对应 MQL 的 `Lots`，同时赋值给 `Strategy.Volume`，便于界面上的手动操作保持
  一致的下单手数。
- **固定止盈。** `TakeProfitPoints` 依据 `Security.PriceStep` 将点数转换为实际价格距离。
- **指标反向立即离场。** 直方图符号翻转会触发即时市价平仓，确保在动能逆转时不会继续持仓。
- **跟踪止损。** 盈利超过设定距离后启动跟踪，并且只在盈利方向移动，从不放宽。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `10` | 每笔市价单的下单手数，同时赋值给 `Strategy.Volume`。 |
| `TakeProfitPoints` | `int` | `10` | 固定止盈的价格步数，低于 10 时策略完全停止交易。 |
| `TrailingStopPoints` | `int` | `25` | 跟踪止损的价格步数，设为 `0` 表示关闭跟踪。 |
| `FastPeriod` | `int` | `9` | MACD 快速 EMA 的长度。 |
| `SlowPeriod` | `int` | `15` | MACD 慢速 EMA 的长度。 |
| `SignalPeriod` | `int` | `8` | MACD 信号线 EMA 的长度。 |
| `ThresholdMultiplier` | `decimal` | `0.004` | 乘以前一根 K 线 ATR 后得到直方图突破阈值。 |
| `AtrPeriod` | `int` | `200` | ATR 波动率过滤器的计算长度。 |
| `CandleType` | `DataType` | 30 分钟周期 | 策略处理的主要时间框架。 |

## 与原始 EA 的差异
- MetaTrader 提供 `AccountFreeMargin()` 并在保证金不足时拒绝交易。StockSharp 无法直接读取该值，因此移除
  了此检查，需要在组合层面另行控制风险。
- MQL 版本通过 `OrderModify` 调整止盈/止损。StockSharp 使用净头寸模式，所以改为在策略内部依据蜡烛高低价
  和跟踪止损变量来管理离场。
- MQL 会统计历史柱数，少于 100 根时打印警告。StockSharp 借助 `BindEx` 自动等待指标形成，无需手工计算
  柱子数量。
- 为了复刻 `Delta` 与 `Delta1` 的比较，移植版本把前一根柱子的 ATR 与直方图缓存成字段，从而避免对指标
  进行随机索引。

## 使用建议
- 请确保 `Security.PriceStep`、`Security.MinVolume` 与 `Security.VolumeStep` 数据准确，以便正确换算仓位和价格
  距离。
- 如果在震荡行情中过度交易，可以提高 `ThresholdMultiplier` 或 `AtrPeriod`；若想更灵敏地捕捉波动扩张，则适当
  降低这两个参数。
- 在高杠杆或高波动品种上运行时应适当下调 `TradeVolume`，因为原始脚本面向大手数的外汇交易。
- 可以利用 `Direction` 属性结合更高周期的趋势过滤，只在特定方向或时段允许开仓。
