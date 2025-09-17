# 仓位规模计算器策略

该示例将 MetaTrader 5 的“Position Size Calculator”面板移植到 StockSharp。策略不会自动下单，
而是根据实时行情与风险参数持续计算推荐手数、风险金额以及保证金需求，并把结果写入日志，
便于在 StockSharp 界面中查看。

## 工作流程

1. 策略订阅 `Level1` 数据，跟踪最新的买一、卖一和成交价。
2. 每收到一次报价更新，就按照方向（多头使用卖一，空头使用买一）确定入场价，并依据
   `Stop Loss (points)` 参数换算出止损价。
3. 账户资金按照下列优先级读取：
   - 当 `Use Equity` 启用时：`Portfolio.CurrentValue` → `CurrentBalance` → `BeginValue`。
   - 当 `Use Equity` 关闭时：`Portfolio.BeginBalance`，若不可用再回退到 `CurrentBalance`、`BeginValue`、`CurrentValue`。
4. 如果 `Use Risk Money` 关闭，则以账户资金乘以 `Risk Percent` 计算风险预算；若开启则直接采用 `Risk Money`。
5. 利用 `Security.PriceStep` 与 `Security.StepPrice` 将止损距离折算成货币，并按合约的交易步长
   对手数取整，确保不低于最小交易量且不超过最大交易量。
6. 把单边佣金参数加到风险金额中（进场与出场各一次），保证金需求则优先读取 `MarginBuy`/`MarginSell`，
   如缺失则以入场价估算。
7. 只有当推荐手数或相关指标发生变化时才写入日志，同时同步到 `Strategy.Volume` 以便外部模块使用。

## 参数说明

| 参数 | 含义 |
|------|------|
| `Stop Loss (points)` | 止损距离，单位为价格点（PriceStep）。 |
| `Use Equity` | 是否使用权益而非初始余额计算风险资本。 |
| `Use Risk Money` | 切换风险输入模式：关闭时使用百分比，开启时使用绝对金额。 |
| `Risk Percent` | 当 `Use Risk Money = false` 时允许亏损的账户百分比。 |
| `Risk Money` | 当 `Use Risk Money = true` 时允许亏损的账户货币金额。 |
| `Commission per Lot` | 每手单边佣金，策略会自动乘以 2 计入进出场费用。 |
| `Trade Direction` | 估算所用的方向（多头使用卖一，空头使用买一）。 |

## 行为特性

- 策略不会提交任何订单，只更新 `Strategy.Volume` 并输出诊断日志。
- 若计算结果低于最小交易量，会直接放弃该建议，以贴近原版工具的行为。
- 在缺失行情或资金信息时不会输出日志，待数据完整后自动恢复。
- 日志内容对应原版界面字段：入场价、止损价、手数、风险金额、风险百分比与保证金。

## 使用建议

- 运行前请连接到目标品种与投资组合。品种需要提供 `PriceStep` 和最好 `StepPrice` 元数据，以保证换算准确。
- 根据券商费用设置 `Commission per Lot`，即可得到更贴近真实的风险数字。
- 策略会把所有关键指标存放在公开的只读属性中（`RecommendedVolume`、`CalculatedRiskMoney`、
  `CalculatedRiskPercent`、`CalculatedMargin`、`LastEntryPrice`、`LastStopPrice`），便于自定义界面或母策略读取。

## 与 MQL 版本的区别

- 图形化界面被 StockSharp 的参数面板取代。
- 风险与保证金信息通过日志呈现，而不是表单控件。
- 手数取整完全依赖 StockSharp 的合约元数据（`VolumeStep`、`MinVolume`、`MaxVolume`），
  与 MetaTrader 对常见外汇/差价合约品种的处理方式一致。
