# OneHrStocTrader 策略

## 概述

**OneHrStocTrader** 策略在 StockSharp 高级 API 中复刻了 MetaTrader 4 专家顾问 *OneHrStocTrader.mq4*。默认在 1 小时周期上运
行，通过随机指标与布林带宽度过滤器的组合寻找入场机会。只有当布林带的上下轨距（以点数衡量）处于指定区间，并且随机指标在
指定小时离开超买/超卖区时，策略才会开仓。

## 交易逻辑

1. **数据**
   - 默认订阅 1 小时 K 线（可配置）。
   - 使用最近完成的 K 线，以匹配 MetaTrader 的执行时机。
2. **布林带过滤**
   - 计算上下轨之间的差值，并换算成点数。
   - 若差值不在 `[BollingerSpreadLower, BollingerSpreadUpper]` 区间内，则忽略所有信号。
3. **随机指标触发**
   - 读取最近两根完成 K 线的随机指标 %K 数值。
   - **做多**：当前 %K 低于 `StochasticLower`，且上一根 %K 抬升（`prev < current`），同时新 K 线的小时等于 `BuyHourStart`。
   - **做空**：当前 %K 高于 `StochasticUpper`，且上一根 %K 下滑（`prev > current`），同时新 K 线的小时等于 `SellHourStart`。
4. **订单管理**
   - 开仓前会平掉反向持仓。
   - `MaxOrdersPerDirection` 限制同向连续入场的次数。
5. **风险控制**
   - 使用固定点数的止盈与止损。
   - 可选的追踪止损，在价格推进足够距离后按点数逐步上移/下移。
   - 每根完成的 K 线都会检查保护价位，一旦触发便以市价平仓。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|--------|
| `TradeVolume` | 下单手数。 | `0.01` |
| `CandleType` | 指标计算所用的时间框架。 | `1h` |
| `BollingerPeriod` | 布林带周期。 | `20` |
| `BollingerSigma` | 布林带标准差倍数。 | `2.0` |
| `BollingerSpreadLower` | 允许的最小布林带宽度（点）。 | `56` |
| `BollingerSpreadUpper` | 允许的最大布林带宽度（点）。 | `158` |
| `BuyHourStart` | 允许做多的小时（0-23）。 | `4` |
| `SellHourStart` | 允许做空的小时（0-23）。 | `0` |
| `StochasticKPeriod` | 随机指标 %K 周期。 | `5` |
| `StochasticDPeriod` | 随机指标 %D 周期。 | `3` |
| `StochasticSlowing` | 随机指标减缓因子。 | `5` |
| `StochasticLower` | 随机指标超卖阈值。 | `36` |
| `StochasticUpper` | 随机指标超买阈值。 | `70` |
| `TakeProfitPips` | 止盈距离（点）。 | `200` |
| `StopLossPips` | 止损距离（点）。 | `95` |
| `TrailingStopPips` | 追踪止损距离（点，0 表示关闭）。 | `40` |
| `MaxOrdersPerDirection` | 同方向连续入场次数上限。 | `1` |

## 图表展示

若运行环境支持图表，策略会绘制：
- K 线价格；
- 布林带；
- 独立面板中的随机指标；
- 实际成交标记。

## 说明

- 点值通过品种的最小价格变动与小数位数推算，复刻原始 EA 中的倍数逻辑。
- 保护价使用 `Security.ShrinkPrice` 进行收敛，确保满足交易所的最小跳动单位。
- 追踪止损只有在价格比前一次保护价多出至少一个点时才会前移，模拟原始 EA 的处理方式。
- 策略仅使用市价单进出场，与源 EA 保持一致。
