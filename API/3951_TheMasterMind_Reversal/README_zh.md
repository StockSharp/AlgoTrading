# MasterMind 反转策略（StockSharp移植版）

## 概览
- 将MetaTrader 4专家顾问“TheMasterMind”移植到StockSharp高阶API，使用随机指标与威廉指标配合捕捉极端反转。
- 通过`SubscribeCandles`和`BindEx`绑定指标，完全基于收盘完成的K线做出决策，复刻原版“在K线收盘时交易”的模式。
- 策略仅交易一个品种，不同时持有双向仓位，收到反向信号时会先平仓再反手。

## 交易逻辑
1. **指标准备**
   - `StochasticOscillator`提供%D信号线，可配置%K/%D平滑长度以及总回溯周期。
   - `WilliamsR`衡量收盘价在近期高低点区间中的相对位置。
2. **入场条件**
   - **做多**：当`%D <= 3`且`Williams %R <= -99.5`，说明市场处于极度超卖区域。
   - **做空**：当`%D >= 97`且`Williams %R >= -0.5`，说明市场处于极度超买区域。
   - 若存在反向仓位，策略先用市价单将其平掉，然后按设定手数建立新的仓位。
3. **出场条件**
   - 反向信号会同时充当出场与反手条件，保持与原版EA相同的“单向仓位”机制。
   - `StartProtection`在策略启动时根据参数自动创建止损、止盈和跟踪止损服务，控制风险。

## 风险管理
- `StopLoss`、`TakeProfit`、`UseTrailingStop`、`TrailingStop`、`TrailingStep`直接映射原EA中的资金管理参数。
- 所有距离均以绝对价格单位表示，可与不同报价精度的券商配合使用；设置为`0`即关闭该保护。
- 只要任意保护参数大于0，`StartProtection`就会被激活。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TradeVolume` | 每次新开仓的基础手数。 | `1` |
| `StochasticPeriod` | 随机指标的总回溯周期。 | `100` |
| `KPeriod` | %K平滑长度。 | `3` |
| `DPeriod` | %D信号长度。 | `3` |
| `WilliamsPeriod` | 威廉指标的回溯周期。 | `100` |
| `StochasticBuyThreshold` | 允许做多的%D阈值。 | `3` |
| `StochasticSellThreshold` | 允许做空的%D阈值。 | `97` |
| `WilliamsBuyLevel` | 威廉指标的超卖水平。 | `-99.5` |
| `WilliamsSellLevel` | 威廉指标的超买水平。 | `-0.5` |
| `StopLoss` | 绝对止损距离。 | `0` |
| `TakeProfit` | 绝对止盈距离。 | `0` |
| `UseTrailingStop` | 是否启用跟踪止损。 | `false` |
| `TrailingStop` | 跟踪止损距离。 | `0` |
| `TrailingStep` | 跟踪止损的步长。 | `0` |
| `CandleType` | 主K线时间框架（默认15分钟）。 | `15分钟` |

## 实现细节
- 使用`BindEx`一次性获取随机指标的`StochasticOscillatorValue`和威廉指标的十进制结果，避免手动访问历史值。
- 所有逻辑都在`candle.State == CandleStates.Finished`且`IsFormedAndOnlineAndAllowTrading()`为真时执行，确保数据完整。
- 若图表服务可用，则调用`DrawCandles`、`DrawIndicator`和`DrawOwnTrades`在图表上呈现行情、指标和成交。
- `LogInfo`输出关键信号，便于回测或实盘监控。
