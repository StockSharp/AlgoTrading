# Exp MA Rounding Candle MMRec 策略

## 概述
**Exp MA Rounding Candle MMRec 策略** 是 MQL5 专家顾问 `Exp_MA_Rounding_Candle_MMRec` 的 StockSharp 版本。原始系统依赖自定义的 “MA Rounding Candle” 指标，该指标会把每根市场 K 线转换为经过平滑处理的合成蜡烛，并根据颜色变化发出信号。C# 实现通过实时重建该指标逻辑并监控颜色流，从而复刻原始策略的行为。

## MA Rounding Candle 的构建流程
1. 每根输入 K 线会分别送入四个相同的移动平均器（针对开盘价、最高价、最低价、收盘价）。支持的平滑类型为 **简单**、**指数**、**平滑 (RMA/SMMA)** 以及 **加权** 移动平均。
2. 原始移动平均值会经过“圆整”过滤器。只有当新值与上一输出值的差异大于 `RoundingFactor * PriceStep` 时才会接受，否则保持前一个圆整值。这一机制与 MQL5 指标完全一致，用于过滤掉细小的噪声。
3. 当真实 K 线的开盘价与收盘价差值小于 `GapSize * PriceStep` 时，会把圆整后的开盘价锁定为上一根圆整蜡烛的收盘价，从而避免极小实体的十字星改变颜色。
4. 圆整完成后，合成蜡烛的颜色定义如下：
   * `2` – 多头蜡烛（圆整开盘价 < 圆整收盘价）
   * `0` – 空头蜡烛（圆整开盘价 > 圆整收盘价）
   * `1` – 中性蜡烛（圆整开盘价 = 圆整收盘价）

策略只保留最近几根颜色（满足信号偏移的需要），不会像指标那样维护完整历史，与原专家保持一致。

## 信号逻辑
信号在每根完成的 K 线结束时进行评估，可通过 `SignalBar` 参数指定偏移：

* `SignalBar` 表示用于触发的历史蜡烛索引（`0` = 当前闭合蜡烛，`1` = 前一根闭合蜡烛，以此类推）。
* 策略还会同时检查前一根蜡烛（`SignalBar + 1`）的颜色。
* 出现 **多头 → 非多头** 的过渡（`color[SignalBar + 1] = 2` 且 `color[SignalBar] != 2`）时：
  * 若启用 `EnableShortExits` 则平掉当前空头；
  * 若启用 `EnableLongEntries` 则按设定手数开多头。
* 出现 **空头 → 非空头** 的过渡（`color[SignalBar + 1] = 0` 且 `color[SignalBar] != 0`）时：
  * 若启用 `EnableLongExits` 则平掉当前多头；
  * 若启用 `EnableShortEntries` 则按设定手数开空头。

平仓操作始终先于开仓执行；若需要反手，策略会把当前仓位的绝对值加到基础下单手数上，使得净仓位准确切换到新方向，这与原 MQL5 版本的处理方式一致。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 1 小时时间框架 | 驱动策略的蜡烛数据序列。 |
| `SmoothingMethod` | `Simple` | 四个圆整序列使用的移动平均类型。 |
| `MaLength` | `12` | 所选移动平均的周期长度。 |
| `RoundingFactor` | `50` | 与 `PriceStep` 相乘得到圆整阈值，数值越大，圆整输出越少变化。 |
| `GapSize` | `10` | 与 `PriceStep` 相乘得到“粘连”阈值，小实体的蜡烛会继承上一圆整收盘价。 |
| `SignalBar` | `1` | 参与信号判断的历史偏移。 |
| `TradeVolume` | `1` | 开新仓时的基础手数，同时会同步到策略的 `Volume` 属性。 |
| `EnableLongEntries` / `EnableShortEntries` | `true` | 是否允许开多 / 开空。 |
| `EnableLongExits` / `EnableShortExits` | `true` | 是否允许平多 / 平空。 |

## 实现细节
* 仅提供 StockSharp 中可用的平滑类型，MQL5 中的 JJMA、JurX、VIDYA、AMA 等特殊算法未被移植。
* 原 EA 中复杂的资金管理重算器被固定手数 `TradeVolume` 取代，便于在 StockSharp 内进行优化与复现。
* 所有基于价格的阈值都会在每次计算时乘以 `Security.PriceStep`，从而自动适配不同品种的最小价格单位。
* 策略通过高阶的 `SubscribeCandles` 订阅接口工作，只处理已完成的蜡烛，与原方案中 `IsNewBar` 的用法保持一致。
* 未实现止盈、止损或其他保护措施，因为这些功能不属于原始专家顾问。

## 使用步骤
1. 选择目标标的并设置合适的 `CandleType`（例如 `TimeSpan.FromHours(1).TimeFrame()`）。
2. 根据需求调整平滑类型、移动平均周期、圆整阈值与 gap 过滤器，使之匹配原 EA 设置或个人优化结果。
3. 设置 `TradeVolume` 为计划交易的手数，该值会自动同步到策略的 `Volume` 属性。
4. 按需启用或关闭多空开仓 / 平仓开关。
5. 启动策略。当 MA Rounding Candle 颜色出现配置的转换时，将会自动下单。

本说明文档对应 `CS/ExpMaRoundingCandleMmrecStrategy.cs` 文件中的实现，可作为在 StockSharp 中使用该策略的参考。
