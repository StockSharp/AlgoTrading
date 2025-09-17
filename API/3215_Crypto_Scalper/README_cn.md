# Crypto Scalper 策略

Crypto Scalper 策略使用 StockSharp 的高级组件还原原始 MetaTrader 专家的逻辑。它在主时间框架上监测快速线性加权
均线的向上或向下穿越，并借助更高时间框架的趋势过滤进行确认。当条件全部满足时，策略通过市价单入场，并用以
MetaTrader 点数表示的止损和止盈距离管理仓位。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Primary Candle` | 主分析时间框架的 K 线类型。 | 1 分钟周期 |
| `Higher Candle` | 用于确认的高阶时间框架 K 线类型。 | 15 分钟周期 |
| `Fast LWMA` | 主时间框架的线性加权移动均线周期。 | 8 |
| `Higher Fast MA` | 高阶时间框架的快速 LWMA 周期。 | 6 |
| `Higher Slow MA` | 高阶时间框架的慢速 LWMA 周期。 | 85 |
| `Momentum Period` | 应用于高阶时间框架的动量指标周期。 | 14 |
| `Momentum Threshold` | 动量值相对基准 (`Momentum Reference`) 的最小偏离。 | 0.3 |
| `Momentum Reference` | 用于模拟 MetaTrader 动量缩放的基准水平。 | 100 |
| `Stop Loss (pips)` | 以 MetaTrader 点数表示的止损距离。 | 20 |
| `Take Profit (pips)` | 以 MetaTrader 点数表示的止盈距离。 | 50 |
| `Volume` | 下单手数（Lots）。 | 0.01 |
| `MACD Fast` | MACD 中的快速 EMA 周期。 | 12 |
| `MACD Slow` | MACD 中的慢速 EMA 周期。 | 26 |
| `MACD Signal` | MACD 信号线 EMA 周期。 | 9 |

## 交易逻辑
1. 订阅主时间框架并计算反应速度较快的 LWMA。
2. 当上一根 K 线向上或向下穿越 LWMA 时生成入场信号。
3. 使用高阶时间框架过滤信号：
   - 做多时高阶快速 LWMA 必须高于慢速 LWMA，做空时相反。
   - MACD 柱值（主线减信号线）在多头情况下需为正值，空头情况下需为负值。
   - 动量值与基准的偏离必须至少达到 `Momentum Threshold`。
4. 在没有其他活动订单且当前仓位允许的情况下发送市价单。
5. 在后续 K 线上检查是否触及止损或止盈，一旦达到目标立即平仓。

## 说明
- 策略完全依赖 StockSharp 的 `Bind` 机制，不直接操作指标缓冲区。
- 每根 K 线都会根据品种的报价步长重新计算保护价位；若步长未配置，则使用后备值 `0.0001`。
- 同一时间仅允许一笔持仓，新信号会在持仓结束之前被忽略。
- C# 实现中的注释全部使用英文，以满足仓库的贡献规范。
