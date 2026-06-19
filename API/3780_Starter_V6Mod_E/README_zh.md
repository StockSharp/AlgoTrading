# Starter V6 Mod E

**Starter V6 Mod E** 是将 MetaTrader 4 专家顾问 `Starter_v6mod_e_www_forex-instruments_info.mq4` 移植到 StockSharp 高阶 API 的版本。该策略保留了原始系统的 Laguerre 极值判定、双 EMA 动量过滤、CCI 过滤以及 EMA 角度门限逻辑，同时按照 StockSharp 事件驱动模型实现交易执行。

## 交易逻辑

- **趋势闸门**：通过 34 周期 EMA 在可配置的起始/结束位移之间计算斜率，并将结果转换为点值。斜率为正时仅允许做多，斜率为负时仅允许做空，斜率近零则禁止新仓。
- **Laguerre 极值**：根据原始递推公式实现的 Laguerre RSI（默认 γ = 0.7），输出范围 0–1。做多要求当前值与上一根 K 线的值均低于 `Laguerre Oversold`，做空要求两者均高于 `Laguerre Overbought`。
- **EMA 动量过滤**：基于 PRICE_MEDIAN 的 120 与 40 周期 EMA 必须同时上升方可做多，同时下降方可做空。
- **CCI 确认**：14 周期 CCI 需低于 `-CCI Threshold` 才能做多，高于 `+CCI Threshold` 才能做空，对应 MQL 脚本中的 `Alpha` 过滤器。
- **周五保护**：超过 `Friday Block Hour` 后禁止开新仓，达到 `Friday Exit Hour` 时强制平掉所有持仓，以规避周末风险。

## 风险管理

- 可配置的止损、止盈及移动止损距离（单位：点），复刻原策略的资金管理行为。
- 移动止损会跟踪持仓以来的最大有利价格，一旦回撤超过设定值即平仓。
- 所有平仓操作均通过高阶 API 的 `SellMarket`/`BuyMarket` 完成，符合平台要求。

## 参数

| 参数 | 说明 |
|------|------|
| `Volume` | 每次市价开仓的交易量。 |
| `StopLossPips` | 止损距离（点）。 |
| `TakeProfitPips` | 止盈距离（点）。 |
| `TrailingStopPips` | 移动止损距离（点，0 表示关闭）。 |
| `SlowEmaPeriod` | 慢速 EMA 的周期（使用 PRICE_MEDIAN）。 |
| `FastEmaPeriod` | 快速 EMA 的周期（使用 PRICE_MEDIAN）。 |
| `AngleEmaPeriod` | 用于角度检测的 EMA 周期。 |
| `AngleStartShift` / `AngleEndShift` | 计算 EMA 斜率时使用的起止位移。 |
| `AngleThreshold` | 允许交易所需的最小斜率（点值）。 |
| `CciPeriod` / `CciThreshold` | CCI 周期及绝对阈值。 |
| `LaguerreGamma` | Laguerre 振荡器的 γ 参数。 |
| `LaguerreOversold` / `LaguerreOverbought` | Laguerre 0–1 区间的超卖/超买阈值。 |
| `CandleType` | 使用的 K 线类型（默认 1 分钟）。 |
| `FridayBlockHour` / `FridayExitHour` | 控制周五风险限制的小时数（本地时间）。 |

## 转换说明

- Laguerre 振荡器直接采用原始递推公式实现，保持 0–1 输出范围与 γ 平滑特性。
- EMA 斜率通过历史 EMA 值的点值差异代替 MQL 中的角度计算函数，实现同样的趋势门限。
- 原始脚本中的资金曲线/网格控制在该版本中未启用，保持与提供的 MT4 设置一致，同时符合 StockSharp 对显式仓位管理的推荐。
- 通过 `OnNewMyTrade` 追踪成交价，用于计算移动止损与持仓最高/最低价。
