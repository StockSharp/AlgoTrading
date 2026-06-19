# AddOn Trailing Stop
[English](README.md) | [Русский](README_ru.md)

移植自 MetaTrader 专家顾问 **AddOn_TrailingStop**。策略本身不会开仓，只负责为当前净持仓更新跟踪止损。

## 工作原理

- 订阅 Level1 数据，实时跟踪最新买价和卖价。
- 根据品种的小数位数计算点值，使参数表现与 MetaTrader 相同（4/5 位小数 = 0.0001，2/3 位小数 = 0.01）。
- 持有多头仓位且买价上涨 `TrailingStartPips` 点时，将内部止损移动到 `Bid - TrailingStartPips` 点。
- 只有当新的止损价格至少比上一位置高 `TrailingStepPips` 点时，才会继续上移。
- 持有空头仓位且卖价下跌 `TrailingStartPips` 点时，将内部止损移动到 `Ask + TrailingStartPips` 点。
- 只有当新的止损价格至少比上一位置低 `TrailingStepPips` 点时，才会继续下移。
- 当当前报价突破跟踪止损时，策略会以市价平掉全部仓位，并重置内部状态。

## 参数

- `EnableTrailing`（默认 **true**）– 是否启用跟踪止损管理。
- `TrailingStartPips`（默认 **15**）– 触发跟踪的盈利点数。
- `TrailingStepPips`（默认 **5**）– 每次继续移动止损所需的额外盈利点数。
- `MagicNumber`（默认 **0**）– 为兼容 MQL 版本保留的标识，在 StockSharp 中仅用于说明。

## 说明

- 需要事先配置 `Security`、`Portfolio`，并接收 Level1 行情。
- 适合与负责开仓的其他策略配合使用。
- 所有输入都通过 `StrategyParam<T>` 暴露，可用于优化或界面调整。
- 当跟踪止损被触发时发送 `BuyMarket`/`SellMarket` 指令，剩余的保护性委托由 StockSharp 自动处理。
