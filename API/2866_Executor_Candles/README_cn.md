# Executor Candles 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader “Executor Candles” 智能交易系统的完整移植版。它监控多种多头与空头反转形态，并可选地通过更高周期的趋势蜡烛确认信号。所有风控机制——止损、止盈与移动止损——都按原策略的“点数”（与品种 price step 相乘）逻辑实现。

## 工作原理

- **趋势过滤**：启用 `UseTrendFilter` 后，策略会读取 `TrendCandleType` 的最新收盘蜡烛。只有当该蜡烛收涨时才允许做多，收跌时才允许做空；默认关闭过滤器，仅依据形态判断。
- **多头形态**：锤子线、看涨吞没、穿头破脚、晨星以及晨星十字星，基于最近三根完成的交易蜡烛。
- **空头形态**：上吊线、看跌吞没、乌云盖顶、暮星以及暮星十字星。
- **仓位管理**：
  - 多、空单分别配置止损与止盈距离（`StopLossBuyPips`、`TakeProfitBuyPips`、`StopLossSellPips`、`TakeProfitSellPips`）。
  - 通过 `TrailingStopBuyPips`、`TrailingStopSellPips` 和 `TrailingStepPips` 控制的移动止损；仅当价格推进了“止损距离 + 步进值”后才会收紧止损，完全复制原版逻辑。
  - `OrderVolume` 指定下单手数，平仓与反手均使用市价单即时完成。

策略订阅 `CandleType` 蜡烛作为主信号源，并在需要时订阅 `TrendCandleType`。内部仅缓存三根完成的蜡烛即可覆盖所有形态，无需长期历史数据。

## 参数

- `CandleType` —— 用于识别形态的交易周期。
- `TrendCandleType` —— 启用趋势过滤时参考的更高周期。
- `OrderVolume` —— 市价单的下单数量。
- `StopLossBuyPips`、`TakeProfitBuyPips`、`TrailingStopBuyPips` —— 多头风险控制参数。
- `StopLossSellPips`、`TakeProfitSellPips`、`TrailingStopSellPips` —— 空头风险控制参数。
- `TrailingStepPips` —— 每次收紧移动止损所需的最小盈利距离。
- `UseTrendFilter` —— 是否启用高周期趋势确认。

## 备注

- 点数类参数会与交易品种的 `PriceStep` 相乘，请确认最小跳动值设置正确。
- 所有判断在蜡烛收盘时进行，盘中数据只会更新当前蜡烛而不会触发立即下单。
- 策略仅使用市价单以保持与原 MetaTrader 智能交易系统一致的执行方式。
