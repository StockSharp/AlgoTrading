# Gandalf PRO 策略

## 概述
Gandalf PRO 策略是 MetaTrader 4 专家顾问 *Gandalf_PRO* 的 StockSharp 版本。原始 EA 通过线性加权均线与递归趋势项
构建自适应平滑滤波器，只要预测价格相对于当前市场价格至少偏移 15 点，就会沿该方向入场，并把止盈设置在预测
值附近、止损放在较远的位置。移植版完全复刻该滤波器和信号逻辑，借助 StockSharp 的高层蜡烛 API，只在收盘后
的完整 K 线基础上做出决策。

## 交易逻辑
1. 订阅 `CandleType` 指定的时间框（默认：1 小时）并仅处理 `CandleStates.Finished` 的蜡烛。
2. 维护一个滚动的收盘价缓存，其长度至少为 `CountBuy` 与 `CountSell` 中的较大值再加一根。
3. 复刻 MQL 中的 `Out()` 函数：先计算移位一根的线性加权均线与简单移动均线，再根据价格因子和趋势因子递归
   生成 `s`、`t` 序列，最终得到预测价格 `s[1] + t[1]`。
4. 多头条件（`EnableBuy` 为真）：
   - 预测价格需高于最新收盘价至少 `15` 点（原 EA 中的 `Bid + 15*x*Point` 判定）。
   - 若当前没有净多头，按 `BaseVolume` 与 `BuyRiskMultiplier` 计算下单数量并买入；如存在净空头则先覆盖再翻多。
   - 将预测值保存为止盈，并把 `BuyStopLossPips` 换算成价格距离设置止损。
5. 空头条件（`EnableSell` 为真）：
   - 预测价格需低于最新收盘价至少 `15` 点。
   - 若当前没有净空头，按配置卖出；如存在净多头则先反手。
   - 保存预测值为止盈，并在市场价上方 `SellStopLossPips` 点处设置止损。
6. 管理持仓：
   - 若蜡烛最低价跌破多头止损或最高价触及多头止盈，则调用 `ClosePosition()` 平掉多单。
   - 若蜡烛最高价突破空头止损或最低价触及空头止盈，同样通过 `ClosePosition()` 平掉空单。
   - 止盈/止损判定基于已完成蜡烛的最高价与最低价。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | 是否允许开多。 |
| `CountBuy` | `int` | `24` | 多头平滑滤波的周期长度。 |
| `BuyPriceFactor` | `decimal` | `0.18` | 递归滤波中当前收盘价的权重。 |
| `BuyTrendFactor` | `decimal` | `0.18` | 递归滤波中趋势项的权重。 |
| `BuyStopLossPips` | `int` | `62` | 多单止损距离，单位为点。 |
| `BuyRiskMultiplier` | `decimal` | `0` | 多单下单前应用于 `BaseVolume` 的乘数（0 表示使用基准手数）。 |
| `EnableSell` | `bool` | `true` | 是否允许开空。 |
| `CountSell` | `int` | `24` | 空头平滑滤波的周期长度。 |
| `SellPriceFactor` | `decimal` | `0.18` | 空头递归滤波中当前收盘价的权重。 |
| `SellTrendFactor` | `decimal` | `0.18` | 空头递归滤波中趋势项的权重。 |
| `SellStopLossPips` | `int` | `62` | 空单止损距离，单位为点。 |
| `SellRiskMultiplier` | `decimal` | `0` | 空单下单前应用于 `BaseVolume` 的乘数。 |
| `BaseVolume` | `decimal` | `1` | 基准下单手数，两侧乘数为 0 时直接使用。 |
| `CandleType` | `DataType` | 1 小时 | 策略订阅并处理的蜡烛序列。 |

## 与原始 MetaTrader EA 的差异
- MT4 允许同时持有独立的 buy 与 sell 订单；StockSharp 采用净持仓模式，因此在开反向仓位前会先平掉现有头寸或
  直接反手。
- 原 EA 的 `lot()` 函数依赖账户可用保证金。移植版提供 `BaseVolume` 与风险乘数，正值表示 `BaseVolume * 乘数`；
  乘数为 0 时保持基准手数。
- 止损/止盈通过监控完成的蜡烛来执行，无法完全复刻 MT4 中由经纪商撮合的即时触发，可能存在细微滑点差异。
- 对于五位小数报价中的 `Digits` 与 `Point` 逻辑，移植版改为根据 `Security.Decimals` 与 `Security.PriceStep`
  自动换算点值。
- 所有指标计算均在 C# 中完成，没有调用 `iMA`。`CalculateTarget` 方法逐项重建了 `Out()` 的递归系数。

## 使用说明
- 启动前务必给策略分配 `Security`；若未设置，将抛出异常以避免在未知品种上运行。
- 根据交易品种调整 `BaseVolume`，再利用风险乘数对多空单分别缩放仓位。
- 至少需要 `max(CountBuy, CountSell) + 1` 根历史蜡烛才能得到有效预测。建议在启动前加载历史数据或先进行预热。
- 15 点的入场缓冲为固定值，可通过增大 `CountBuy`/`CountSell` 或调整权重来改变信号频率与敏感度。
- 因为止盈/止损依赖蜡烛极值，低时间框虽然反应更快，但对历史数据与滑点的要求也更高。

## 实现细节
- 使用 `SubscribeCandles()` 并绑定到 `ProcessCandle`，确保所有判断只基于已经完成的 K 线。
- 仅维护一段收盘价列表，并在需要时重算递归 `s`、`t` 序列，以复刻 MQL 中的平滑滤波器。
- 通过 `Security.PriceStep` 与小数位数把点值转换成绝对价格差，对应原代码中的 `x * Point` 计算。
- 当价格触及保护水平时调用 `ClosePosition()`，在发送新的方向信号前先扁平当前净仓位。
