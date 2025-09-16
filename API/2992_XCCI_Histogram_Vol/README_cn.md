# XCCI Histogram Vol 策略

## 概览
本策略基于 MetaTrader 专家顾问 `Exp_XCCI_Histogram_Vol`，在 StockSharp 平台上重现其逻辑。核心思想是使用自定义的 “XCCI Histogram Vol” 指标：将 CCI 指标值乘以成交量，并通过可选择的移动平均线平滑，然后与根据成交量动态缩放的阈值比较。策略完全采用高阶 API，只处理收盘蜡烛，并保留原策略的双分批入场设计。

## 指标流程
1. 使用配置的周期计算 CCI 值。
2. 将 CCI 值与当前蜡烛的成交量相乘。
3. 分别对 CCI×Volume 序列和原始成交量序列应用所选移动平均线（支持 `Simple`、`Exponential`、`Smoothed`、`Weighted`、`Hull`、`VolumeWeighted`）。
4. 以平滑后的成交量为基准，按比例放大四个阈值系数（HighLevel2/1 与 LowLevel1/2）。
5. 根据平滑后的 CCI×Volume 值与阈值的关系划分五个区域：`0` 极强多头、`1` 多头、`2` 中性、`3` 空头、`4` 极强空头。

策略会记录每根收盘蜡烛对应的区域。`SignalBarOffset` 参数用于指定延迟多少根已完成的蜡烛再进行交易判断，对应原始指标中的 `SignalBar` 设置。

## 交易规则
- **多头平仓**：若评估区域为 `3` 或 `4`，立即关闭所有多头仓位。
- **空头平仓**：若评估区域为 `1` 或 `0`，立即关闭所有空头仓位。
- **第一档多头开仓**：当当前区域变为 `1` 且更早一根蜡烛的区域大于 `1` 时触发，表示价格从中性或空头区进入多头区。使用 `PrimaryEntryVolume` 指定的手数开仓，并在有空头仓位时先反手。
- **第二档多头开仓**：当当前区域变为 `0` 且更早一根蜡烛的区域大于 `0` 时触发，表示价格冲入极强多头区，使用 `SecondaryEntryVolume`。
- **第一档空头开仓**：当当前区域变为 `3` 且更早一根蜡烛的区域小于 `3` 时触发，代表刚进入空头区。使用 `PrimaryEntryVolume`，若当前持有多头则先平仓。
- **第二档空头开仓**：当当前区域变为 `4` 且更早一根蜡烛的区域小于 `4` 时触发，代表极强空头动能，使用 `SecondaryEntryVolume`。

策略在净头寸穿越 0 时会重置各档位的触发标记，从而模拟 MetaTrader 中两个不同魔术号码的行为——同一档位在被平仓前不会重复加仓。

## 风险控制
- `UseStopLoss` / `UseTakeProfit` 可分别启用基于点差的固定止损/止盈距离，内部通过 `StartProtection` 调用实现，可选配置。
- 所有操作均使用市价单，遵循 StockSharp 平台的滑点与成交控制。
- 日志会记录每一笔交易的触发原因，方便复盘与排查。

## 参数说明
- **CciPeriod** – CCI 指标的计算周期。
- **MaLength** – 两个平滑移动平均的长度。
- **HighLevel2 / HighLevel1 / LowLevel1 / LowLevel2** – 乘以平滑成交量后形成的自适应阈值。
- **SignalBarOffset** – 延迟的收盘蜡烛数量（0 表示使用最新收盘，1 表示上一根，以此类推）。
- **Smoothing** – 平滑所用的移动平均类型（StockSharp 中提供的子集：SMA、EMA、SMMA、WMA、Hull MA、VWMA）。
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – 独立控制开仓与平仓方向。
- **PrimaryEntryVolume / SecondaryEntryVolume** – 两个分批档位对应的下单手数，对多头与空头共用。
- **UseStopLoss / StopLossPoints** – 可选的绝对止损设置。
- **UseTakeProfit / TakeProfitPoints** – 可选的绝对止盈设置。
- **CandleType** – 订阅的蜡烛类型或时间周期。

## 与原版的差异
- 仅提供 StockSharp 现成的平滑方法，原策略中的 JJMA、JurX、ParMA、VIDYA、AMA 等特殊算法未实现，可选择最接近的替代方案。
- 成交量取自 `ICandleMessage.TotalVolume`，未模拟 Tick Volume；若行情源只提供成交笔数，结果会与原平台略有差异。
- StockSharp 采用净头寸模型，而不是多个独立订单。通过档位标记实现类似的分批行为，并确保与平台的执行模型兼容。
