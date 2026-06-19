# iCCI iRSI 策略

## 概述
**iCCI iRSI Strategy** 源自 MetaTrader 5 智能交易程序 `iCCI iRSI.mq5`。原版策略结合 CCI 与 RSI 两个振荡指标，在两者同时进入超买或超卖区域时发出信号，并立即设置止损、止盈以及可选的移动止损。本次移植在 StockSharp 中复现了所有核心要素：以“点”（pip）为单位的参数、自动平掉反向仓位以及可选的信号反转模式。

## 交易逻辑
1. 订阅指定的蜡烛周期，分别计算 `CciPeriod` 周期的 `CommodityChannelIndex` 与 `RsiPeriod` 周期的 `RelativeStrengthIndex`。
2. 只在蜡烛收盘后评估信号，完全对应 MQL 脚本里对新棒的等待逻辑。
3. 当 CCI 与 RSI 同时低于各自的下阈值（`CciLowerLevel`、`RsiLowerLevel`）时开多或翻多；当两者同时高于上阈值（`CciUpperLevel`、`RsiUpperLevel`）时开空或翻空。将 `ReverseSignals` 设为 `true` 可反向解读信号。
4. 下单前若存在反向持仓，会先市价平仓，确保净持仓方向与当前信号一致。
5. 入场后在随后每根蜡烛的收盘价上检查止损与止盈。两个距离参数仍以“点”为单位，通过 `PriceStep` 转换为实际价格。若标的的 `Decimals` 为 3 或 5，则额外乘以 10，以匹配 MT5 中对 fractional pip 的处理方式。
6. `TrailingStopPips` 大于 0 时启用移动止损：仅当盈利幅度超过 `TrailingStopPips + TrailingStepPips` 时才会沿价格方向收紧止损，并遵循最小移动步长。

## 风险控制
- **止盈 / 止损**：可选的点数距离，成交后立即转换成价格水平。若在蜡烛收盘时达到任一水平，则立即以市价出场。
- **移动止损**：复刻原策略的逻辑——只有当盈利足够大并超过最小步长时，才会上调（或下调）止损。
- **下单量**：`TradeVolume` 参数取代原 EA 的“固定手数 / 百分比风险”开关。需要动态资金管理时，可结合优化器或其他风险模块。
- **仓位管理**：新信号出现时强制平掉反向仓位，保持持仓干净，与原函数 `ClosePositions` 一致。

## 参数说明
- **Candle Type**：信号计算所使用的蜡烛类型（默认 1 小时）。
- **CciPeriod**：CCI 周期（默认 14）。
- **CciUpperLevel / CciLowerLevel**：CCI 超买 / 超卖阈值（默认 +80 / −80）。
- **RsiPeriod**：RSI 周期（默认 42）。
- **RsiUpperLevel / RsiLowerLevel**：RSI 触发阈值（默认 60 / 30）。
- **ReverseSignals**：是否反转信号方向（默认 `false`）。
- **TradeVolume**：市价单手数（默认 0.1，对应 MT5 输入）。
- **StopLossPips / TakeProfitPips**：止损 / 止盈距离（默认 0 与 140，设置为 0 即关闭）。
- **TrailingStopPips / TrailingStepPips**：移动止损距离与最小步长（默认 5 / 5；若距离为 0 则不启用移动止损）。

## 实现细节
- 使用 StockSharp 自带的 `CommodityChannelIndex` 与 `RelativeStrengthIndex` 指标，并通过 `Bind` 高级 API 直接接收十进制数值，省去了 MQL 中的缓冲拷贝。
- 订单管理在蜡烛收盘时执行，与原版 `PrevBars` 变量的保护逻辑一致，避免单根 K 线内的重复交易。
- Pip 转换逻辑遵循 MT5：在报价小数位为 3 或 5 的情况下，对 `PriceStep` 乘以 10，以获得标准点值。
- 保护性止损/止盈在策略内部以市价平仓模拟，因为在 StockSharp 的回测环境中无法直接修改经纪商订单。
- 策略会自动创建展示区，将价格、CCI、RSI 画在独立面板上，方便验证信号。

## 与原版 EA 的差异
- 未移植 `MoneyFixedMargin` 模块，仓位完全由 `TradeVolume` 控制。
- 无法访问 MT5 的 `FreezeStopsLevels`，移动止损仅按照价格距离与步长判断。
- 移除了原脚本中的日志与弹窗提示，需要时可接入 StockSharp 自带的日志系统。
- 止损与止盈的触发在蜡烛收盘时检查，而不是像 MT5 那样依赖撮合即时触发，这使得回测更加确定。

## 使用建议
1. 建议先在 1 小时时间框架上运行，以保持与原策略一致；更低周期虽然信号更多，但噪声也更大。
2. CCI 与 RSI 的上下阈值应同时调优，只有两者一致才会产生信号。
3. 在外汇品种上运行时，请确认证券对象提供了正确的 `PriceStep` 与 `Decimals`，以免点值转换错误。
4. 若偏好突破策略，可启用 `ReverseSignals`；保持默认值则按照经典反转逻辑运行。
5. 如需账户级风控，可结合 StockSharp 的权益保护、回撤限制等模块，替代原 EA 的 `m_money` 组件。

以上内容为在 StockSharp 平台部署、调试以及扩展 iCCI iRSI 策略提供了完整说明。
