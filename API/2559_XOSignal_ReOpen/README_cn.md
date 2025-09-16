# XOSignal Re-Open 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 中复刻 MetaTrader 专家顾问 *Exp_XOSignal_ReOpen*，并使用高层 API 实现。算法基于所选品种与周期的K线，通过 ATR(13) 构建的 XO 型突破指示器来生成交易信号。出现上箭头时平掉空单、可选择开多，并在价格每向有利方向移动固定 tick 数时加仓；下箭头对空头执行同样的逻辑。每一层加仓都会附带以 tick 为单位的固定止损与止盈。

## 核心逻辑

- 构建宽度为 `Range * PriceStep` 的 XO 通道。突破通道将重置边界并确定当前趋势方向。
- ATR(13) 决定箭头离当根K线的距离：多头箭头位于 `Low - ATR * 3/8`，空头箭头位于 `High + ATR * 3/8`。
- 仅处理收盘后的K线，可通过 `SignalBar` 参数将信号延后若干根。

## 入场规则

- **开多**：当出现上箭头、允许多头入场（`EnableBuyEntries = true`）、没有持仓或空头已平且该信号尚未执行时，以 `Volume` 为数量买入。
- **多头加仓**：持有多单期间，每当收盘价较最近加仓价上涨 `PriceStepTicks` 个 tick，就再买入一次，最多达到 `MaxPyramidingPositions` 层，并同步更新保护性止损/止盈。
- **开空与加仓**：与多头逻辑完全镜像。

## 出场规则

- **信号出场**：上箭头在 `EnableSellExits = true` 时平掉所有空单；下箭头在 `EnableBuyExits = true` 时平掉所有多单。
- **风险控制**：所有持仓层级共用 `StopLossTicks` 和 `TakeProfitTicks` 指定的 tick 距离。当当根K线触及该水平时，头寸被全部平仓。
- **方向切换**：出现相反方向的入场信号时，会先平掉已有仓位再建立新方向。

## 仓位管理

- 每笔订单的数量由 `Volume` 决定。
- 止损与止盈以 tick 表示，设置为 0 可关闭该防护。
- 当仓位全部出场后，加仓计数会重置，下一次信号重新从第一层开始。

## 参数

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `Volume` | 每次下单数量 | `1` |
| `StopLossTicks` | 止损 tick 距离（0 表示关闭） | `1000` |
| `TakeProfitTicks` | 止盈 tick 距离（0 表示关闭） | `2000` |
| `PriceStepTicks` | 有利方向移动多少 tick 后加仓 | `300` |
| `MaxPyramidingPositions` | 含首单在内的最大分层数量 | `10` |
| `EnableBuyEntries` / `EnableSellEntries` | 允许开多 / 开空 | `true` |
| `EnableBuyExits` / `EnableSellExits` | 允许在反向箭头时平多 / 平空 | `true` |
| `CandleType` | 计算使用的K线周期 | `H4` |
| `Range` | XO 盒子的高度（tick） | `10` |
| `AppliedPrice` | XO 指标取值类型 | `Close` |
| `SignalBar` | 信号延后执行的已收盘K线数量 | `1` |

在实际使用前请根据交易品种的波动幅度调整 tick 型参数，并确认品种的最小报价步长设置正确。
