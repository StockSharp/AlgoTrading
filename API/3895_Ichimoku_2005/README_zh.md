# Ichimoku 2005 策略

该策略是 MetaTrader 专家顾问 `ichimok2005` 的移植版本，针对 StockSharp 的高层 API 进行了重写。它专注于识别价格对 Ichimoku 指标 Senkou Span B 的突破，并通过连续的实体 K 线确认动能。

## 交易逻辑

### 做多条件
1. 检查最近 `Shift + 2` 根已完成的 K 线（默认 `Shift = 1`，因此会评估前三根柱）。
2. 需要满足：
   - 最早的参考柱（`Shift + 2`）开盘价低于 Senkou Span B。
   - 中间参考柱（`Shift + 1`）开盘价高于 Senkou Span B 且收盘价同样高于该线。
   - 最近参考柱（`Shift`）开盘价和收盘价都在 Senkou Span B 之上。
   - 最近两根参考柱均为阳线（收盘价高于开盘价）。
3. 当 Senkou Span A 低于 Senkou Span B 时，Chinkou Span 不能位于云层内部，以避免进入震荡区间。
4. 如果当前持有空头仓位则先行平仓；若上一信号不是多头，则开立新的多单。

### 做空条件
1. 规则与做多相反：
   - `Shift + 2` 柱开盘价高于 Senkou Span B。
   - `Shift + 1` 柱开盘价和收盘价低于 Senkou Span B。
   - `Shift` 柱开盘价和收盘价低于 Senkou Span B。
   - 最近两根参考柱均为阴线（收盘价低于开盘价）。
2. 当 Senkou Span A 低于 Senkou Span B 时，Chinkou Span 必须处于云层之外。
3. 若存在多头仓位则平仓；若上一信号不是空头，则开立新的空单。

止损与止盈以价格步长（PriceStep）的倍数指定，并自动转换为绝对距离，同时通过 `StartProtection` 注册市场保护单，以贴近原版 EA 的处理方式。

## 仓位控制

原始 EA 支持两种头寸规模模式：
- **固定手数**（`UseMoneyManagement = false`）：使用参数 `OrderVolume`（默认 0.1 手）下单。
- **资金管理**（`UseMoneyManagement = true`）：根据账户当前资产和 `MaximumRisk` 百分比计算下单手数，并按照合约的最小变动手数进行取整，保证不低于一个最小步长。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `StopLossPoints` | 止损距离（价格步长数）。 | 30 |
| `TakeProfitPoints` | 止盈距离（价格步长数）。 | 60 |
| `Shift` | 检测突破时的历史偏移量。 | 1 |
| `OrderVolume` | 关闭资金管理时使用的固定手数。 | 0.1 |
| `MaximumRisk` | 启用资金管理时，使用的账户风险百分比。 | 10 |
| `UseMoneyManagement` | 是否启用资金管理。 | false |
| `TenkanPeriod` | Ichimoku Tenkan-sen 周期。 | 9 |
| `KijunPeriod` | Ichimoku Kijun-sen 周期。 | 26 |
| `SenkouBPeriod` | Ichimoku Senkou Span B 周期。 | 52 |
| `CandleType` | 计算所用的时间框架（默认 1 小时 K 线）。 | 1 小时 |

## 备注

- 策略仅处理已完成的 K 线，确保 Ichimoku 指标数值稳定可靠。
- `_lastSignal` 用于防止连续重复执行同方向信号，保持与原始 EA 一致的行为。
- 如果合约没有提供 `PriceStep`，止损和止盈距离会被视为绝对价格差。
