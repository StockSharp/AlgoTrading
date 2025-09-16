# Color Ma RSI Trigger Duplex 策略

该策略将 **Exp_ColorMaRsi-Trigger_Duplex.mq5** 专家顾问迁移到 StockSharp 高级 API。
系统包含两个彼此独立的 MaRsi-Trigger 模块：**多头模块** 负责管理多头仓位，**空头模块** 负责管理空头仓位。
每个模块都会读取自定义指标的取值，该指标只能返回 `+1`（看涨）、`0`（中性）或 `-1`（看跌）。
迁移版本完整保留了 MT5 原版的行为，包括针对已完成两根 K 线后才触发的确认机制，以及多空方向独立的资金管理参数。

## 交易思想

1. 对每个模块分别计算两条指数移动平均线（快线与慢线）以及两条 RSI（快线与慢线），使用用户选择的 K 线数据。
2. 指标在每根完结 K 线时输出：当快线同时高于慢线时得到 `+1`，当快线同时低于慢线时得到 `-1`，其余情况返回 `0`。
   为了与 MT5 指标一致，输出被限制在 `[-1, 1]` 区间内。
3. 策略保存指标的历史值。设定 `SignalBar` 偏移量后，会比较 `SignalBar + 1` 根之前的取值（记为 `older`）与 `SignalBar` 根之前的取值（记为 `recent`）。
4. 多头逻辑：
   - 如果 `older < 0`，并且允许关闭多头，则立即平掉现有多头仓位；
   - 如果 `older > 0` 且 `recent <= 0`，并且允许开仓，则准备开多。
5. 空头逻辑完全对称：
   - 如果 `older > 0`，在允许的情况下平掉现有空头仓位；
   - 如果 `older < 0` 且 `recent >= 0`，在允许的情况下开空。
6. 如果设置了止损或止盈（以价格步长表示），当 K 线触及对应水平时仓位会被平仓。

多头与空头模块可以订阅不同的时间框架和价格类型，从而重现原始 EA 的双时间框架结构，或探索新的组合。

## 参数

| 参数 | 说明 |
|------|------|
| `LongCandleType`, `ShortCandleType` | 多头、空头模块使用的 K 线类型（默认 4 小时）。 |
| `LongVolume`, `ShortVolume` | 每次开仓时提交的市场订单量。 |
| `LongAllowOpen`, `ShortAllowOpen` | 是否允许对应模块开仓。 |
| `LongAllowClose`, `ShortAllowClose` | 是否允许对应模块平仓。 |
| `LongStopLossPoints`, `ShortStopLossPoints` | 止损距离（单位：价格步长，0 表示关闭）。 |
| `LongTakeProfitPoints`, `ShortTakeProfitPoints` | 止盈距离（单位：价格步长，0 表示关闭）。 |
| `LongSignalBar`, `ShortSignalBar` | 用于信号判断的历史 K 线偏移量。 |
| `LongRsiPeriod`, `LongRsiLongPeriod`, `ShortRsiPeriod`, `ShortRsiLongPeriod` | 快/慢 RSI 的周期。 |
| `LongMaPeriod`, `LongMaLongPeriod`, `ShortMaPeriod`, `ShortMaLongPeriod` | 快/慢移动平均线的周期。 |
| `LongRsiPrice`, `ShortRsiPrice` | 快速 RSI 使用的价格类型。 |
| `LongRsiLongPrice`, `ShortRsiLongPrice` | 慢速 RSI 使用的价格类型。 |
| `LongMaPrice`, `ShortMaPrice` | 快速移动平均线使用的价格类型。 |
| `LongMaLongPrice`, `ShortMaLongPrice` | 慢速移动平均线使用的价格类型。 |
| `LongMaType`, `ShortMaType` | 快速移动平均线的类型（SMA、EMA、SMMA、WMA）。 |
| `LongMaLongType`, `ShortMaLongType` | 慢速移动平均线的类型。 |

## 交易规则

1. 等待所选 K 线序列开始产生已完成的蜡烛，并确保所有指标已形成。
2. 每根完结 K 线更新指标值并写入历史缓存。
3. 当历史数据长度不少于 `SignalBar + 2` 时，按照“交易思想”部分的条件判定多空信号。
4. 若准备开多而当前持有空头仓位（或反之），并且允许平仓，则先平掉反向仓位，再开新仓。
5. 每根 K 线都检查止损/止盈；若达到阈值则立即平仓。
6. 通过 `BuyMarket` 和 `SellMarket` 方法提交市场单。

## 风险控制

* 止损/止盈使用 `Security.PriceStep` 计算。若交易品种未提供价格步长，则默认使用 `1`。
* 多头与空头各自拥有独立的止损、止盈设置。
* 策略不包含额外的保护机制（例如移动止损），与 MT5 原版行为保持一致。

## 说明

* 在 MT5 中，订单会被安排在下一根 K 线开盘时触发；在 StockSharp 中，策略在蜡烛收盘后立即下单，效果等同。
* 原版 EA 支持多种资金管理模式（`LOT`、`BALANCE` 等）。在 StockSharp 中，直接使用固定成交量参数 `LongVolume`/`ShortVolume` 更为自然。
* MT5 专用的滑点控制与魔术号逻辑在本策略中已省略。
* 指标计算依赖 StockSharp 内置的 MA 与 RSI 实现，最终结果会被限制在 `[-1, 1]` 区间内，以匹配原始 `ColorMaRsi-Trigger` 指标。
