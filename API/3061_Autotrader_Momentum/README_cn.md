# Autotrader Momentum 策略

## 概述
**Autotrader Momentum 策略** 是对 MetaTrader 5 专家顾问 *Autotrader Momentum (barabashkakvn 版本)* 的移植。策略通过比较监控 K 线与历史参考 K 线的收盘价来识别动量方向：当收盘价高于参考值时判定为多头动量，低于参考值时判定为空头动量，并立即在市场价执行订单。实现完全基于 StockSharp 的高级 API，保持了原脚本“新 K 线触发”的处理方式。

为保持与 MQL 脚本一致的点值控制，止损、止盈与跟踪止损均以“点 (pip)”为单位配置，并根据交易品种的 `PriceStep` 自动换算为价格偏移量。当报价精度为 3 位或 5 位小数时，会额外乘以 10，复现原策略对三位/五位报价的点值调整。每根完成的 K 线在判断新信号之前都会先执行跟踪止损与保护性退出逻辑，确保风险控制优先。

## 交易流程
1. 订阅配置的 `CandleType`，仅处理状态为 `Finished` 的 K 线，模拟 EA 只在新 K 线生成时做出决策。
2. 维护一个长度为 `max(CurrentBarIndex, ComparableBarIndex) + 1` 的收盘价窗口。
3. 计算监控 K 线 (`CurrentBarIndex`，默认 0) 与历史参考 K 线 (`ComparableBarIndex`，默认 15) 的收盘价差。
4. 若监控收盘价高于参考收盘价，则平掉所有空头仓位并按配置的交易量开多。
5. 若监控收盘价低于参考收盘价，则平掉所有多头仓位并按配置的交易量开空。
6. 每次开仓都会重新计算加权平均建仓价，并刷新止损、止盈和跟踪止损价格。

StockSharp 采用净头寸模型，因此在反向信号出现时，会先补足相反方向的持仓量，再加上配置的基础交易量，实现与 MQL 版本“先平后开”的效果。

## 参数说明
- `CandleType` – 用于比较的 K 线类型，默认 1 小时。
- `TradeVolume` – 每次信号使用的基础成交量，反手时还会加上对冲所需的量。
- `StopLossPips` – 止损距离（点）。设为 0 可关闭固定止损。
- `TakeProfitPips` – 止盈距离（点）。设为 0 可关闭固定止盈。
- `TrailingStopPips` – 跟踪止损距离（点）。设为 0 可关闭跟踪止损。
- `TrailingStepPips` – 推进跟踪止损所需的最小有利波动（点），启用跟踪止损时必须大于 0。
- `CurrentBarIndex` – 监控 K 线的索引，0 表示最新完成的 K 线。
- `ComparableBarIndex` – 用于比较的历史 K 线索引。

所有以点为单位的设置都会依据 `PriceStep` 换算成真实价格偏移。当最小报价单位代表三位或五位小数时，会乘以 10 来模拟 MetaTrader 中的点值定义。

## 风险控制
- **固定止损/止盈：** 当 `StopLossPips` 或 `TakeProfitPips` 大于 0 时，策略会基于加权建仓价维护对应的止损、止盈价位。
- **跟踪止损：** 当 `TrailingStopPips` 与 `TrailingStepPips` 均大于 0 时启用。只有当价格相对建仓价的有利波动超过 `TrailingStopPips + TrailingStepPips` 时，才会推进止损，复现原脚本中“移动前需足够波动”的限制。
- **状态清理：** 当仓位被策略或外部操作清零时，会立即清空缓存的止损/止盈信息，避免遗留无效价格水平。

## 实现细节
- 仅使用 StockSharp 的高层 API（`BuyMarket`、`SellMarket`），遵循移植规范，不额外维护指标集合。
- 通过滚动列表缓存收盘价，使 `CurrentBarIndex` 与 `ComparableBarIndex` 可在运行时调整。
- 由于采用净头寸模式，多次同向加仓会实时重新计算加权建仓价，再据此刷新风险参数。
- 在每根 K 线的信号评估之前先执行跟踪止损与保护性退出，避免在已触发离场的 K 线上重复开仓。

## 原始策略信息
- **来源：** `MQL/22409/Autotrader Momentum.mq5`
- **作者：** barabashkakvn（MetaTrader 社区）
