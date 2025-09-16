# RSI EA 策略
[English](README.md) | [Русский](README_ru.md)

RSI EA 策略复现了 MetaTrader 5 中的 “RSI EA” 智能交易系统。策略在所选 K 线序列上监控相对强弱指数（RSI），当动量穿越可调的超买或超卖阈值时采取操作。转换版本保留了原始系统的止损、止盈、移动止损以及自动资金管理思想，并采用 StockSharp 的高层策略 API 实现。

## 策略逻辑

### 指标
- **RSI**：周期可调，基于选定的蜡烛类型计算。

### 入场条件
- **做多**：RSI 从下方向上穿越 `RsiBuyLevel`（上一根 RSI 低于阈值，本根高于阈值），且允许做多。
- **做空**：RSI 从上方向下穿越 `RsiSellLevel`（上一根 RSI 高于阈值，本根低于阈值），且允许做空。

策略仅保持单一净头寸，在已有头寸时不会再开立对冲方向的仓位。

### 离场条件
- **信号离场**：当 `CloseBySignal` 开启时，RSI 反向穿越立即平掉当前持仓。
- **保护性止损**：`StopLoss` 大于零时，策略监控入场均价与当前价格的距离，当亏损达到设定值时平仓。
- **止盈**：`TakeProfit` 大于零时，在达到目标距离后平仓。
- **移动止损**：`TrailingStop` 大于零时，止损会跟随价格移动。做多时，当价格至少向有利方向移动 `TrailingStop` 距离后，止损上移至 `收盘价 - TrailingStop`；做空时采用对称规则。

### 仓位大小
- `UseAutoVolume = true` 时，根据账户权益与风险计算下单量：`Volume = Equity * RiskPercent / (100 * stopDistance)`，其中 `stopDistance` 优先使用 `StopLoss`，若未设置则使用 `TrailingStop`。若缺少任何保护距离，则退回使用手工仓位。
- `UseAutoVolume = false` 时，所有订单均使用固定的 `ManualVolume` 数量。

## 参数
- `CandleType`：用于计算指标的蜡烛类型（默认 1 分钟）。
- `RsiPeriod`：RSI 计算窗口长度（默认 14）。
- `RsiBuyLevel`：触发做多与平空的超卖阈值（默认 30）。
- `RsiSellLevel`：触发做空与平多的超买阈值（默认 70）。
- `EnableLong`：是否允许做多（默认 true）。
- `EnableShort`：是否允许做空（默认 true）。
- `CloseBySignal`：是否在 RSI 反向穿越时平仓（默认 true）。
- `StopLoss`：以价格单位表示的止损距离（默认 0，关闭）。
- `TakeProfit`：以价格单位表示的止盈距离（默认 0，关闭）。
- `TrailingStop`：以价格单位表示的移动止损距离（默认 0，关闭）。
- `UseAutoVolume`：是否启用基于风险的仓位控制（默认 true）。
- `RiskPercent`：自动仓位时使用的权益风险百分比（默认 10）。
- `ManualVolume`：关闭自动仓位时的固定下单量（默认 0.1）。

## 实现细节
- 使用 `SubscribeCandles(...).Bind(...)` 高层接口，让 RSI 指标直接将数值传入策略，无需手工处理指标缓冲区。
- 当持仓归零时会清除所有缓存的止损与止盈水平，避免旧值残留。
- 移动止损逻辑遵循原始 MQL 代码：只有当价格相对当前止损前进超过两倍的跟踪距离时才会上调或下调止损，以避免过早收紧。
- StockSharp 运行于净头寸模式，因此无法像原始 EA 那样同时持有多空仓位。策略会等待当前仓位平掉后再开反向单。
- 自动仓位计算需要 `StopLoss` 或 `TrailingStop` 中至少一个有效；若无法确定风险距离，则使用手工仓位。

## 默认配置
- 时间框架：1 分钟蜡烛。
- RSI：周期 14，阈值 30/70。
- 资金管理：启用自动仓位，风险 10% 权益，备用手工数量 0.1。
- 风险控制：默认未启用止损、止盈或移动止损（实盘前请自行配置）。

## 使用建议
- 根据交易品种与周期设置合适的 `CandleType`，策略可在 StockSharp 支持的任何时间框架运行。
- 在启用自动仓位前，请先设定合理的 `StopLoss` 或 `TrailingStop`，保证风险计算有意义。
- 代码中已调用 `StartProtection()`，建议保持启用，以减少连接中断或孤立头寸的风险。
- 在不同市场上应用时，应持续跟踪成交表现，并根据波动性调整 RSI 阈值。
