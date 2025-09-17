# Lego EA 策略
[English](README.md) | [Русский](README_ru.md)

**Lego EA Strategy** 将 MetaTrader 的 Lego EA 专家顾问迁移到 StockSharp。策略可以按“积木”方式组合多个过滤器（CCI、双均线、随机指标、加速指标、DeMarker、Awesome Oscillator），并且可以分别为开仓和平仓启用或禁用每个过滤器，从而灵活重现原始逻辑或构建自定义版本。

## 参数
- `Volume`：上一笔交易获利时使用的基础手数。
- `LotMultiplier`：上一笔亏损后应用的手数放大倍数。
- `StopLossPips`：止损距离（点数，将自动换算为价格）。
- `TakeProfitPips`：止盈距离（点数）。
- `UseCciForEntry` / `UseCciForExit`：是否在开仓/平仓时启用 CCI 过滤。
- `UseMaForEntry` / `UseMaForExit`：是否启用快慢均线交叉过滤。
- `UseStochasticForEntry` / `UseStochasticForExit`：是否使用随机指标和阈值过滤。
- `UseAcceleratorForEntry` / `UseAcceleratorForExit`：是否检查 Accelerator Oscillator 的加速模式。
- `UseDemarkerForEntry` / `UseDemarkerForExit`：是否检查 DeMarker 指标是否进入超买/超卖区。
- `UseAwesomeForEntry` / `UseAwesomeForExit`：是否使用 Awesome Oscillator 动量确认。
- `CciPeriod`：CCI 周期。
- `MaFastPeriod`、`MaSlowPeriod`：快、慢均线周期。
- `MaShift`：均线的横向偏移量（向后回溯的已完成 K 线数量）。
- `MaMethod`：均线平滑方式。
- `MaPrice`：均线计算使用的价格类型。
- `StochasticKPeriod`、`StochasticDPeriod`、`StochasticSlow`：随机指标参数。
- `StochasticLevelUp`、`StochasticLevelDown`：随机指标超卖/超买阈值。
- `DemarkerPeriod`、`DemarkerLevelUp`、`DemarkerLevelDown`：DeMarker 设置。
- `CandleType`：指标使用的蜡烛周期。

## 交易流程
1. 每当蜡烛收盘时，策略更新所有已启用过滤器的指标值。
2. 指标信号基于上一根完整蜡烛计算（与原 EA 使用 `iGetArray(...,1)` 的方式一致）。
3. 若所有启用的开仓过滤器同时给出看多信号，则允许做多；若全部给出看空信号，则允许做空。
4. 当没有持仓且满足信号条件时，下达市价单。手数为基础 `Volume`，或在亏损后按 `LotMultiplier` 放大。
5. 当持有仓位时，只有当所有启用的平仓过滤器同时指示反向信号时才会平仓。
6. `StartProtection` 根据 `StopLossPips`、`TakeProfitPips` 和合约最小价格步长自动设置止损/止盈。

## 风险与资金管理
- 获利后，下一笔订单恢复为基础手数。
- 亏损后，下一笔订单的手数会乘以 `LotMultiplier`，复现原 EA 的递增手法。
- 在发送订单前，会根据交易所限制对手数进行步长、最小值和最大值校验。

## 与 MetaTrader 版本的差异
- 指标价格源映射到 StockSharp 内置类型：CCI 使用典型价格，均线使用 `MaPrice` 指定的价格。
- 所有计算都基于已完成的蜡烛，避免未完成数据造成的偏差。
- 冻结区间与手动止损/止盈处理由 `StartProtection` 负责。
- 只有当仓位完全平掉后，才更新最近交易的盈亏状态，等价于 MT5 中处理 `DEAL_ENTRY_OUT` 的逻辑。

## 使用建议
- 推荐先使用默认配置（仅启用均线过滤），确认行为与原 EA 一致后再逐步增加过滤器。
- 调高 `LotMultiplier` 会显著放大连续亏损时的风险，请留意保证金占用。
- 可结合 Backtester 对不同过滤器组合和周期进行回测验证。

当前尚未提供 Python 版本。
