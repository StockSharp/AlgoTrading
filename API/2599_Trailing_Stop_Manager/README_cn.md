# 跟踪止损管理器
[English](README.md) | [Русский](README_ru.md)

移植自 MetaTrader 专家顾问 **Exp_TrailingStop**。该策略仅负责为外部开仓的持仓维护跟踪止损，不会自行产生交易信号。

## 工作原理

- 订阅 Level1 行情，实时接收最新的买价 (Bid) 与卖价 (Ask)。
- 当持有多头仓位且 Ask 相对开仓价盈利 `TrailingStartPoints` 个最小价位后，将止损调整为 `Ask - StopLossPoints * PriceStep`。
- 只有当新的多头止损价至少比旧止损价高出 `TrailingStepPoints` 个最小价位时才会更新，避免回撤时下移。
- 当持有空头仓位且 Bid 相对开仓价盈利 `TrailingStartPoints` 个最小价位后，将止损调整为 `Bid + StopLossPoints * PriceStep`。
- 只有当新的空头止损价比旧止损价至少低 `TrailingStepPoints` 个最小价位时才会更新。
- 如果最新买价或卖价穿越当前止损位，策略会以市价平掉全部仓位并清空内部状态。
- 在仓位清零或方向翻转时（多转空或空转多）重置所有跟踪参数。

## 参数说明

- `StopLossPoints`（默认 **1000**）– 市价与跟踪止损之间的距离，单位为最小价位。
- `TrailingStartPoints`（默认 **1000**）– 启动跟踪止损所需的盈利距离，单位为最小价位。
- `TrailingStepPoints`（默认 **200**）– 每次调整止损所需的最小增量，单位为最小价位。
- `PriceDeviationPoints`（默认 **10**）– 为保持与 MQL 版本一致而保留，在 StockSharp 中未使用，因为滑点处理逻辑不同。

所有参数均通过 `StrategyParam<T>` 暴露，可在界面或优化器中调整。

## 其他说明

- 必须设置有效的 `Security` 对象，并确保其 `PriceStep` 大于零且可以接收 Level1 数据。
- 策略只依赖报价更新，对时间框架没有限制，适用于日内或波段环境。
- 可以与其他入场策略或人工交易结合使用，仅负责仓位的动态止损管理。
- 实现中只保存当前止损值，不存储历史集合，保持与原始 MQL 脚本相同的轻量特性。
- 通过调用 `SellMarket` / `BuyMarket` 平仓，利用 StockSharp 的内部保护机制，无需像 MQL 那样发送带偏差的修改请求。
