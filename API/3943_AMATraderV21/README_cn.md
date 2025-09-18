# AMA Trader v2.1 策略

AMA Trader v2.1 策略移植自 MetaTrader 4 专家顾问 **AMA_TRADER_v2_1.mq4**。它将考夫曼自适应移动平均线（Kaufman AMA）的爆发行情与双重平滑的 Heiken Ashi 过滤器、RSI 动量确认结合在一起。

## 核心逻辑

1. **自适应趋势过滤**：自定义 AMA 引擎完整复刻原指标，包括快/慢常数、效率比以及幂次参数 `AmaPower`。当 AMA 值相对于上一根柱子跳跃超过 `AmaThreshold` 个价格步长时，视为趋势爆发。
2. **Heiken Ashi 确认**：先对原始 OHLC 价格进行一次可配置均线平滑，再对构建出的 Heiken Ashi 缓冲区进行第二次平滑。平滑后的蜡烛收盘价高于开盘价视为看涨，仅允许做多；反之则允许做空。
3. **RSI 动量过滤**：使用可配置周期的 RSI。做多时要求 RSI 在 70 以下并且相较上一值回落；做空时要求 RSI 在 30 以上并且相较上一值回升。
4. **仓位管理**：同一时间只持有一个方向的仓位，可选止损/止盈（按价格步长）、可选移动止损，并在 RSI 穿越 70/30 极值时执行可选的部分平仓，然后等待下一次极值信号完成全部离场。

## 参数

| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 15 分钟 | 计算所用的 K 线周期。 |
| `TradeVolume` | 0.1 | 每次进场使用的基础手数。 |
| `AmaLength` | 9 | AMA 的回溯长度。 |
| `AmaFastPeriod` | 2 | AMA 的快速常数（以柱数计）。 |
| `AmaSlowPeriod` | 30 | AMA 的慢速常数。 |
| `AmaPower` | 2 | 平滑常数的幂次，对应 MQ4 参数 `G`。 |
| `AmaThreshold` | 2 步 | 触发信号所需的最小 AMA 变化量（价格步长）。 |
| `FirstMaMethod` | Smoothed | 第一次平滑原始 OHLC 的均线类型。 |
| `FirstMaPeriod` | 6 | 第一次平滑的周期。 |
| `SecondMaMethod` | LinearWeighted | 第二次平滑 Heiken Ashi 缓冲区的均线类型。 |
| `SecondMaPeriod` | 2 | 第二次平滑的周期。 |
| `RsiPeriod` | 14 | RSI 的计算周期。 |
| `PartialClosePercent` | 70% | RSI 极值触发时的部分平仓比例，0 表示禁用。 |
| `StopLossSteps` | 50 | 止损距离，按价格步长表示，0 表示禁用。 |
| `TakeProfitSteps` | 100 | 止盈距离，按价格步长表示，0 表示禁用。 |
| `TrailingSteps` | 30 | 移动止损距离，按价格步长表示，0 表示禁用。 |

## 交易规则

- **做多**：AMA 变动为正且超过 `AmaThreshold`，最新平滑 Heiken Ashi 蜡烛为看涨，同时 RSI 低于 70 且相较上一值回落时开多。
- **做空**：AMA 变动为负且超过 `AmaThreshold`，最新平滑 Heiken Ashi 蜡烛为看跌，同时 RSI 高于 30 且相较上一值回升时开空。
- **部分平仓**：若启用，在 RSI 上穿 70（多单）或下穿 30（空单）时按 `PartialClosePercent` 比例减仓。
- **完全离场**：RSI 反向穿越 70/30、触发止损/止盈或移动止损时平掉剩余仓位。

实现使用 StockSharp 高级 API，通过蜡烛订阅驱动自定义 AMA 计算器、Heiken Ashi 平滑链路以及 RSI 指标。代码中的注释全部为英文，以符合转换规范。
