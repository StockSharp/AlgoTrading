# 货币强度 v1.1 策略

## 概述
货币强度 v1.1 策略复现了 MetaTrader 专家顾问 *Currency Strength v1.1* 的核心思想。策略使用 26 个主要及交叉外汇货币对的日线百分比涨跌幅来衡量八大主要货币（USD、EUR、JPY、CAD、AUD、NZD、GBP、CHF）的相对强弱。当两种货币的强弱差值超过阈值时，策略会在对应的货币对上按强势方向开仓。

## 市场与数据
- **交易标的：** 26 个主要外汇货币对（USDJPY、USDCAD、AUDUSD、USDCHF、GBPUSD、EURUSD、NZDUSD、EURJPY、EURCAD、EURGBP、EURCHF、EURAUD、EURNZD、AUDNZD、AUDCAD、AUDCHF、AUDJPY、CHFJPY、GBPCHF、GBPAUD、GBPCAD、GBPJPY、CADJPY、NZDJPY、GBPNZD、CADCHF）。
- **数据周期：** 日线 (D1)。策略仅处理已经完成的 K 线，保证计算一致性。
- **所需字段：** 每根 K 线的开盘价、最高价、最低价、收盘价。

## 货币强度计算
每个货币对的日内涨跌幅按以下公式计算：

```
(change) = (Close − Open) / Open × 100
```

随后根据原始 EA 的公式组合成各货币强度：

- **EUR 强度** = EURJPY、EURCAD、EURGBP、EURCHF、EURAUD、EURUSD、EURNZD 的平均值
- **USD 强度** = USDJPY、USDCAD、–AUDUSD、USDCHF、–GBPUSD、–EURUSD、–NZDUSD 的平均值
- **JPY 强度** = USDJPY、EURJPY、AUDJPY、CHFJPY、GBPJPY、CADJPY、NZDJPY 的平均值取相反数
- **CAD 强度** = CADCHF、CADJPY、–GBPCAD、–AUDCAD、–EURCAD、–USDCAD 的平均值
- **AUD 强度** = AUDUSD、AUDNZD、AUDCAD、AUDCHF、AUDJPY、–EURAUD、–GBPAUD 的平均值
- **NZD 强度** = NZDUSD、NZDJPY、–EURNZD、–AUDNZD、–GBPNZD 的平均值
- **GBP 强度** = GBPUSD、–EURGBP、GBPCHF、GBPAUD、GBPCAD、GBPJPY、GBPNZD 的平均值
- **CHF 强度** = CHFJPY、–USDCHF、–EURCHF、–AUDCHF、–GBPCHF、–CADCHF 的平均值

所有平均数均与原 EA 保持相同的权重构成。

## 交易规则
1. 当 26 个货币对全部生成新的已完成日线后，重新计算所有货币强度。
2. 对于每个货币对，比较其基准货币与计价货币的强度差。如果绝对值大于 `DifferenceThreshold`，则产生交易信号。
3. 信号方向遵循强弱关系：
   - 基准货币强于计价货币 → 做多该货币对。
   - 基准货币弱于计价货币 → 做空该货币对。
4. 仅当日线蜡烛与信号方向一致时才允许下单（收盘价高于开盘价才能买入，收盘价低于开盘价才能卖出），这与原 EA 的趋势过滤条件保持一致。
5. 策略遵循净持仓模式。如果反向信号出现且存在相反持仓，会先平掉原仓位再以市价反向开仓。
6. 若启用 `TradeOncePerDay`，每个货币对每天最多只允许一次多头入场和一次空头入场。

## 风险管理与离场
- `UseSlTp` 选项可以启用日线级别的止损与止盈检查，距离以点（pip）为单位，通过 `StopLossPips` 与 `TakeProfitPips` 设置。
- 保护逻辑使用最新日线的最高价/最低价判断是否触达止盈或止损，触发后在下一次评估时以市价平仓。
- 若未启用止损止盈，仓位将保持不变，直到出现反向信号或手动停止策略，这与原 EA 的行为一致。

## 参数说明
| 参数 | 描述 |
|------|------|
| `CandleType` | 使用的 K 线周期（默认：日线）。 |
| `DifferenceThreshold` | 触发交易所需的最小强度差（百分比点）。 |
| `TradeOncePerDay` | 若为 `true`，限制每个货币对每天仅进多一次、进空一次。 |
| `UseSlTp` | 启用基于日线的止损/止盈逻辑。 |
| `TakeProfitPips` | 止盈点数。 |
| `StopLossPips` | 止损点数。 |
| 货币对参数 | 26 个货币对的 `Security` 输入，启动前必须全部指定。 |
| `Volume` | 基类属性，定义下单手数（默认 0.01 手）。 |

## 实现细节
- 策略使用高级 API 的 `SubscribeCandles` 为每个货币对单独订阅日线数据。
- 仅处理 `CandleStates.Finished` 的蜡烛，符合 StockSharp 的迁移要求。
- 在所有货币对的交易日同步前不会触发信号，确保货币篮子计算的一致性。
- 内部字典记录每个方向的最近入场日期，并保存持仓的入场信息以供止损止盈使用。

## 使用建议
1. 在启动策略前为 26 个参数全部指定对应的证券，缺失将抛出异常以避免部分计算。
2. 确保数据源可以为所有配置的货币对提供日线蜡烛，以维持强度计算同步。
3. 调整 `DifferenceThreshold` 以控制交易频率，阈值越小信号越多但可能带来更多反向交易。
4. 根据经纪商报价精度调整点差止损止盈距离，默认假设支持小数点后第五位的报价。
