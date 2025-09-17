# EMA RSI 波动自适应均线交叉策略
[English](README.md) | [Русский](README_ru.md)

本策略是 MetaTrader 专家顾问 **EA_MARSI_1-02** 的直接移植版本。它监听两条 *EMA_RSI_VA* 指标线的交叉情况 —— 该指
标由 Integer 编写，利用 RSI 推动 EMA 的自适应周期。每当慢线与快线交叉，策略都会立即翻转净头寸，以完全再现原始
EA 的“信号即反手”逻辑，并遵循 StockSharp 的下单规范。

## 指标工作方式

原始 MQL 套件包含自定义指标 `EMA_RSI_VA`。它在价格上构建 EMA，并根据 RSI 偏离 50 的幅度动态调整平滑周期。本移
植版本实现了 `EmaRsiVolatilityAdaptiveIndicator` 类来还原该公式：

1. 在选定的 `AppliedPrice` 价格源上计算周期为 `RSIPeriod` 的 RSI。
2. 计算 RSI 与 50 的距离 `|RSI - 50| + 1`，作为波动度代理。
3. 构造自适应系数
   `multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`。
4. 使用该系数乘以基础 EMA 周期，得到动态周期 `pdsx`。
5. 以平滑系数 `2 / (pdsx + 1)` 执行标准 EMA 递推，并使用所选的价格类型作为输入。

当 RSI 远离 50 时，动态周期缩短，指标反应更快；RSI 平稳时，周期拉长以抑制噪声。慢线与快线均支持
`StockSharp.Messages.AppliedPrice` 的全部价格模式。

## 交易规则

- **信号判断**
  - *做空/卖出*: 前一根 K 线的慢线 < 快线，且当前慢线 ≥ 快线。
  - *做多/买入*: 前一根 K 线的慢线 > 快线，且当前慢线 ≤ 快线。
- **执行细节**
  - 只处理已完成的蜡烛（`CandleStates.Finished`）。
  - 触发信号时提交一笔市价单，其数量可同时平掉原有仓位并建立新的反向仓位。
  - 体量会自动匹配 `Security.MinVolume`、`Security.VolumeStep` 与 `Security.MaxVolume` 等交易所限制。
- **翻转**
  - 通过一次 `SellMarket` 或 `BuyMarket` 调整净头寸穿过零点，从而模拟 EA 在出现反向信号时立即反手的行为。

## 风险管理

- `TakeProfitPoints` 与 `StopLossPoints` 对应 EA 中的 TP/SL（以价格点表示）。任一参数大于零时会启动 StockSharp 的保护
  管理器，使用绝对价差和 `useMarketOrders = true`，模拟原程序中不断修改止损/止盈的逻辑。
- `UseBalanceMultiplier` 实现了 `use_Multpl`。启用后实际下单量变为
  `Volume * PortfolioEquity / MaxDrawdown`，随后再按交易所要求进行归一化。
- 依旧调用 `StartProtection()`，方便外部模块挂接跟踪止损或保本逻辑。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Volume` | `0.1` | 启动时的基础下单手数，未应用资金乘数前的数值。 |
| `TakeProfitPoints` | `0` | 止盈距离（点）；`0` 表示不启用。 |
| `StopLossPoints` | `0` | 止损距离（点）；`0` 表示不启用。 |
| `UseBalanceMultiplier` | `false` | 是否启用资金规模乘数，等价于 EA 的 `use_Multpl`。 |
| `MaxDrawdown` | `10000` | 资金乘数的分母，对应 EA 的 `Max_drawdown`。 |
| `SlowRsiPeriod` | `310` | 慢线 RSI 周期。 |
| `SlowEmaPeriod` | `40` | 慢线的基础 EMA 周期。 |
| `SlowAppliedPrice` | `Close` | 慢线使用的价格类型。 |
| `FastRsiPeriod` | `200` | 快线 RSI 周期。 |
| `FastEmaPeriod` | `50` | 快线的基础 EMA 周期。 |
| `FastAppliedPrice` | `Close` | 快线使用的价格类型。 |
| `CandleType` | `TimeFrame(1m)` | 进行计算的蜡烛序列。 |

## 实现说明

- 采用 StockSharp 高层 API（`SubscribeCandles().Bind(...)`）完成订阅与指标绑定，无需手动遍历历史缓存。
- 仅使用已完成的蜡烛，行为与 MQL 中 `CopyBuffer(..., 1, 2, ...)` 的读法一致。
- 订单量通过 `Security.MinVolume`、`Security.VolumeStep`、`Security.MaxVolume` 进行归一化，避免提交无效委托。
- 根据要求暂不提供 Python 版本，目录内只有 C# 实现和多语言文档。

该移植版本忠实重现原始 EA 的交易逻辑，同时提供更符合 StockSharp 生态（Designer、Runner 等）的参数界面与风控选项。
