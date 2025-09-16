# Basic CCI RSI 策略
[English](README.md) | [Русский](README_ru.md)

Basic CCI RSI 策略重现了原始 MetaTrader 智能交易系统：只有当商品通道指数（CCI）和相对强弱指数（RSI）在连续两根收盘 K 线上同时给出同向信号时才入场。StockSharp 版本保留了以点数（pips）表示的资金管理规则，会自动把这些距离换算为价格步长，并实现了与 MQL5 代码中 `Trailing()` 函数一致的移动止损机制。

## 运行流程

1. 每根 K 线收盘（默认 1 小时）后，策略会获取最新的 CCI 与 RSI 数值。
2. 做多条件：在当前与上一根收盘 K 线上，CCI 与 RSI 均高于各自的上轨阈值。做空条件：两根收盘 K 线上两个指标都低于各自的下轨阈值。
3. 出现信号时，策略按设定手数开仓（必要时先平掉反向仓位），并按照点数距离计算固定的止损与止盈价格。
4. 持仓期间，策略持续检查 K 线的最高价与最低价，一旦触及止损或止盈水平，就以市价立即离场。
5. 移动止损完全复制原始 EA：当浮动利润超过 `TrailingStopPips + TrailingStepPips` 时，将止损移动到距离当前收盘价 `TrailingStopPips` 点的位置（多单向下、空单向上）。再次移动止损前，需要额外获得 `TrailingStepPips` 点的利润。

这种结构在 StockSharp 中复刻了原有 EA 的逻辑，同时利用了平台的高层蜡烛订阅与指标系统。

## 风险控制

- **止损**：按点数设定，并转换为标的的价格步长；当参数为 0 时停用。
- **止盈**：按点数设定，并转换为价格步长；为 0 时禁用。
- **移动止损**：带缓冲区的点数追踪，行为与 MQL5 源代码一致；当 `TrailingStopPips` 为 0 时不启用。
- **仓位大小**：通过策略的 `Volume` 属性设置，默认交易 1 手。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `StopLossPips` | 入场价与止损价之间的点数距离。 |
| `TakeProfitPips` | 入场价与止盈价之间的点数距离。 |
| `TrailingStopPips` | 启动移动止损所需的盈利点数。 |
| `TrailingStepPips` | 每次再次收紧止损前所需的额外盈利点数。 |
| `CciPeriod` | CCI 的计算周期。 |
| `RsiPeriod` | RSI 的计算周期。 |
| `RsiLevelUp` | 多头确认所需的 RSI 上限阈值。 |
| `RsiLevelDown` | 空头确认所需的 RSI 下限阈值。 |
| `CciLevelUp` | CCI 多头确认阈值。 |
| `CciLevelDown` | CCI 空头确认阈值。 |
| `CandleType` | 用于聚合蜡烛并计算指标的时间框架。 |

## 默认参数

- `StopLossPips` = 125
- `TakeProfitPips` = 60
- `TrailingStopPips` = 5
- `TrailingStepPips` = 5
- `CciPeriod` = 12
- `RsiPeriod` = 15
- `RsiLevelUp` = 75
- `RsiLevelDown` = 30
- `CciLevelUp` = 80
- `CciLevelDown` = -95
- `CandleType` = 1 小时蜡烛

## 其他说明

- 当交易品种价格保留 3 位或 5 位小数时，策略会自动把价格步长乘以 10，与 MetaTrader 中的 “adjusted point” 逻辑一致。
- 仅在收盘蜡烛上生成信号，完全符合原始 EA “只在新柱上计算” 的要求，并避免重绘。
- 平仓始终使用市价单，这让 StockSharp 的回测结果更加可重复。

## 分类标签

- 类别：振荡指标确认
- 方向：可做多亦可做空
- 指标：CCI、RSI
- 止损类型：固定点差 + 移动止损
- 复杂度：入门级
- 周期：日内/波段（默认 1 小时）
- 季节性：无
- 神经网络：无
- 背离识别：无
- 风险水平：中等
