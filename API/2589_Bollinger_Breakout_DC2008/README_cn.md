# DC2008 布林带突破策略
[English](README.md) | [Русский](README_ru.md)

将 Sergey Pavlov (DC2008) 的 MetaTrader 布林带突破专家顾问移植到 StockSharp 高级策略 API。策略在选定的周期上等待完整蜡烛，依据指定的价格源计算布林带，并且只有在当前持仓不亏损时才开仓或反向。

## 概览
- 在设定的 `CandleType` 上计算布林带（支持收盘价、开盘价、最高价、最低价、中位价、典型价、加权价或 OHLC 平均价）。
- **做多** 条件：蜡烛最低价跌破下轨，同时最高价仍低于中轨，表示价格被压制在均线下方后可能反弹。
- **做空** 条件：蜡烛最高价突破上轨，同时最低价仍高于中轨，表示价格被推升到均线上方后可能回落。
- 原始 MQL 策略在每个 tick 上运行；移植版本在蜡烛完成后判断，保证指标输出稳定且避免未完成数据。
- 仅当当前仓位的浮动盈亏不为负时才允许开新仓或反向，保持与原始过滤规则一致。

## 交易流程
### 指标处理
1. 订阅所选的蜡烛类型（默认 1 小时）。
2. 将 `AppliedPrice` 指定的价格输入布林带指标 (`Length = BandsPeriod`, `Width = BandsDeviation`)。
3. 仅在指标给出有效的上轨、中轨、下轨后才继续判断信号。

### 信号判定
- **买入**：`Low < LowerBand` 且 `High < MiddleBand`。
- **卖出**：`High > UpperBand` 且 `Low > MiddleBand`。

### 仓位管理
- 无持仓时，在信号出现后按 `Volume` 下达市价单建仓。
- 已有持仓时：
  - 计算蜡烛收盘价下的浮动盈亏 `Position * (Close - PositionPrice)`。
  - 如果浮亏为负，则跳过本根蜡烛的所有操作。
  - 如果浮盈不为负且信号与当前方向相反，则按 `Volume + |Position|` 发送市价单，实现平仓并反向建仓。
  - 与当前方向相同的信号不会加仓。
- 策略不设置固定止损或止盈，退出完全依赖满足条件的反向信号。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `BandsPeriod` | 80 | 布林带均线与标准差的计算周期，可优化，必须大于 0。 |
| `BandsDeviation` | 3.0 | 布林带标准差系数，控制带宽，可优化。 |
| `AppliedPrice` | Close | 指定指标使用的价格：Close、Open、High、Low、Median、Typical、Weighted 或 Average (OHLC/4)。对应 MetaTrader 的 `ENUM_APPLIED_PRICE`。 |
| `CandleType` | 1 小时 | 用于分析和下单的蜡烛类型，可替换为 StockSharp 支持的其他数据类型。 |
| `Volume`（继承） | 取决于经纪商 | 新开仓的数量，反向时会自动加上当前持仓的绝对值。 |

## 与原始 MQL EA 的差异
- Tick 级触发 → 仅在蜡烛收盘后触发。
- 原代码中的布林带移位参数固定为 0，因此在此实现中保持隐式。
- MQL 中直接读取持仓浮盈；此版本通过 `PositionPrice` 与收盘价估算，足以判断正负号。
- 去除了订单文本注释，仅保留核心交易逻辑。

## 实现说明
- 使用 StockSharp 的高层 API：`SubscribeCandles().Bind(...)`、`BuyMarket`、`SellMarket` 等。
- 如果 UI 中可创建图表区域，将自动绘制蜡烛、布林带以及策略成交。
- 每次启动都会重新创建指标，因此修改参数后在下次运行即可生效。
