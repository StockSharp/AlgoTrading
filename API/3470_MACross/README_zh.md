# MACross 策略

该策略将 `MQL/34176/MACross.mq4` 专家顾问移植到 StockSharp 高级 API。核心思想仍是双均线交叉开平仓，并保留所有以点数和账户权益表示的风控规则。

## 交易逻辑

1. 在选定的蜡烛类型上计算两条简单移动平均线（SMA）：
   - `FastPeriod` 用于捕捉快速变化。
   - `SlowPeriod` 描述中期趋势。
2. 每当一根蜡烛收盘时比较两条均线：
   - 当快速均线上穿慢速均线时建立多头仓位，若之前存在空头则先行平仓。
   - 当快速均线下穿慢速均线时建立空头仓位，若之前存在多头则先行平仓。
3. 所有订单均按 `LotSize` 指定的基准手数下单，并根据合约限制（`VolumeStep`、`MinVolume`、`MaxVolume`）自动对齐。
4. 建仓后策略会同时跟踪两个以点数表示的风险目标。点值首先依据 `Security.Decimals` 推断，若无法获取则退回使用 `PriceStep`：
   - `TakeProfitPips` 定义了止盈距离，触发后立即平仓锁定利润。
   - `StopLossPips` 定义了止损距离，触发后立即平仓限制亏损。
5. `MinEquity` 参数用于权益保护。当组合价值低于阈值时，策略继续管理已有仓位，但不再开立新的头寸。

所有判断都只基于已完成的蜡烛，与原始 EA 在新 K 线形成后再执行逻辑的方式完全一致。

## 可视化

若界面支持图表，策略会绘制：

- 订阅到的蜡烛序列；
- 快速与慢速 SMA；
- 策略自身的成交记录，方便核对交叉信号。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `8` | 快速 SMA 的长度，用于产生入场信号。 |
| `SlowPeriod` | `int` | `20` | 慢速 SMA 的长度，代表趋势方向。 |
| `TakeProfitPips` | `decimal` | `20` | 止盈距离（点）。点值会根据标的的小数位自动推断。 |
| `StopLossPips` | `decimal` | `20` | 止损距离（点）。采用与止盈相同的点值计算方式。 |
| `LotSize` | `decimal` | `1` | 基础下单手数，发送订单前会按交易所要求取整。 |
| `MinEquity` | `decimal` | `100` | 最低账户权益。权益低于该值时禁止开仓。 |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | 用于计算 SMA 并触发信号的蜡烛序列。 |

## 与 MQL 版本的差异

- 原 EA 在 `OrderSend` 中将止损/止盈参数设为 0。本移植版本改为在每根蜡烛收盘时手动检测价格是否触发止损或止盈。
- `cekMinEquity` 现在读取 `Portfolio.CurrentValue` 与 `Portfolio.BeginValue` 来判断权益，逻辑与 `AccountEquity()` 等价。
- 点值计算遵循 `GetPipPoint` 的规则：两位或三位小数使用 0.01，四位或五位小数使用 0.0001，其他情况退回到 `PriceStep`。

这些改动保证策略既忠于原始逻辑，又能充分利用 StockSharp 在可视化与风险控制方面的功能，并支持对所有参数进行优化。
