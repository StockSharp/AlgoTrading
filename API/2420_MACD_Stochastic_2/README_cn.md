# MACD Stochastic 2 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 StockSharp 高级 API 复刻 MetaTrader 中的“MACD Stochastic 2”专家逻辑。通过 MACD 主线的三根 K 线形态与随机指标 Stochastic 结合，识别接近超卖或超买区域的动量反转。同时为多空方向分别设置止损、止盈，并提供可选的点（pip）单位追踪止损。

## 概览

- 通过参数 `CandleType` 指定任意品种和时间框架。
- 仅依据 MACD 主线判断局部高低点，信号线和柱状图可用于图表展示。
- 入场时要求 Stochastic %K 低于 20（做多）或高于 80（做空）。
- 点值计算遵循原版 EA：取合约的 `PriceStep`，若价格精度为 3 或 5 位小数则额外乘以 10。

## 交易逻辑

### 多头入场

1. 当前及前两根已完成 K 线的 MACD 主线值全部小于 0。
2. 当前 MACD 值大于上一根，上一根小于前两根（形成局部底部）。
3. Stochastic %K 低于 20（超卖）。
4. 若存在空头仓位则先平仓，随后在 `Position <= 0` 时开多。

### 空头入场

1. 当前及前两根已完成 K 线的 MACD 主线值全部大于 0。
2. 当前 MACD 值小于上一根，上一根大于前两根（形成局部顶部）。
3. Stochastic %K 高于 80（超买）。
4. 若存在多头仓位则先平仓，随后在 `Position >= 0` 时开空。

### 仓位管理

- **固定止损 / 止盈：** 多空方向分别配置点数距离，设置为 0 可关闭对应防护。
- **追踪止损：** 启用后，当价格推进超过设定距离时生效；只有当盈利超过追踪步长时才上调/下调止损，减少频繁修改。
- **反向信号：** 出现反向条件时先平掉当前仓位，再用设定手数开立新的反向仓位。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | `1` | 新仓下单量。 |
| `StopLossBuyPips` | `50` | 多头止损点数（0 表示关闭）。 |
| `StopLossSellPips` | `50` | 空头止损点数（0 表示关闭）。 |
| `TakeProfitBuyPips` | `50` | 多头止盈点数（0 表示关闭）。 |
| `TakeProfitSellPips` | `50` | 空头止盈点数（0 表示关闭）。 |
| `TrailingStopPips` | `0` | 追踪止损距离（点）。0 表示禁用。 |
| `TrailingStepPips` | `5` | 更新追踪止损所需的最小盈利点数；启用追踪时必须为正。 |
| `MacdFastPeriod` | `12` | MACD 快速 EMA 长度。 |
| `MacdSlowPeriod` | `26` | MACD 慢速 EMA 长度。 |
| `MacdSignalPeriod` | `9` | MACD 信号线平滑周期。 |
| `StochasticKPeriod` | `5` | Stochastic %K 回溯周期。 |
| `StochasticDPeriod` | `3` | Stochastic %D 平滑周期。 |
| `StochasticSlowing` | `3` | Stochastic %K 额外平滑长度。 |
| `CandleType` | `1 小时周期` | 指标计算所用的 K 线类型（时间框架）。 |

## 说明

- Pip 计算方式：`pip = PriceStep`，若报价精度为 3 或 5 位小数则乘以 10，与原始脚本保持一致。
- Stochastic 阈值 20/80 在代码中写成常量，需要自定义时可直接修改源代码。
- 策略仅在完整收盘的 K 线上做出决策，行为与 MetaTrader 的收盘执行一致。

## 使用步骤

1. 启动前配置交易标的、`CandleType` 和下单量。
2. 根据波动率调整止损、止盈和追踪止损参数。
3. 需要优化时，可借助 StockSharp 优化器搜索 MACD 与 Stochastic 参数组合。
4. 若界面存在图表区域，策略会自动绘制 K 线、MACD、Stochastic 以及成交标记，便于监控。
