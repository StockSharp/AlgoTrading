# Nirvaman Imax 策略

## 概述
Nirvaman Imax 策略是 MetaTrader 4 专家顾问 `NirvamanImax.mq4` 的 StockSharp 版本，同时重现了随附的 HA、Moving Averages2 与 iMAX3alert 指标。该实现保留了原策略“海龟蜡烛 + 双相趋势探测器 + EMA 基线过滤”的核心思想，并借助高阶 API 自动在持仓达到设定时间后平仓。

## 指标与过滤条件
- **Heikin-Ashi 蜡烛**：复现原始 HA 指标，通过比较 Heikin 开盘价与收盘价判断多空实体。
- **快/慢 EMA 交叉**：用一组 EMA 代替 MT4 中 iMAX3alert1 的双色缓冲区。快线向上穿越慢线时生成看涨信号，反向穿越时生成看跌信号。
- **EMA 趋势过滤**：等价于 Moving Averages2 指标的 EMA 输出，只允许在价格位于滤波线同侧时开仓。
- **时间过滤**：若蜡烛时间的小时数落在 `NoTradeStartHour` 与 `NoTradeEndHour` 之间（支持跨午夜窗口与经纪商时区偏移），则跳过该信号。
- **持仓时间限制**：`CloseAfter` 到期后强制平仓，对应原脚本中的 `tiempoCierre` 逻辑。
- **止损与止盈**：按照合约最小跳动价差（PriceStep）转换为价格距离，填 0 可关闭该保护。

## 交易规则
1. 等待 Heikin-Ashi、快 EMA、慢 EMA 与过滤 EMA 均形成，并且存在上一根 K 线收盘价。
2. 如当前时间位于禁止交易时段，则直接返回。
3. 做多条件：
   - 当前蜡烛快 EMA 向上穿越慢 EMA；
   - Heikin-Ashi 收盘价高于开盘价（多头实体）；
   - 上一根蜡烛收盘价高于 EMA 过滤线。
4. 做空条件与之相反：快 EMA 下穿慢 EMA、Heikin-Ashi 为空头实体、上一根收盘价低于 EMA 过滤线。
5. 平仓条件：
   - 蜡烛最高/最低触及止盈或止损；
   - 持仓时间超过 `CloseAfter`；
   - 平台的保护模块（`StartProtection()`）请求退出时立即平仓。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 每次开仓的基础成交量。 | `0.1` |
| `CandleType` | 计算信号所用的时间框架。 | `30` 分钟 |
| `FastTrendLength` | 模拟 iMAX 蓝色缓冲的快 EMA 长度。 | `10` |
| `SlowTrendLength` | 模拟 iMAX 红色缓冲的慢 EMA 长度。 | `21` |
| `FilterLength` | 基线 EMA（Moving Averages2 等价物）周期。 | `13` |
| `StopLoss` | 止损距离（以价格跳动为单位），0 表示不启用。 | `50` |
| `TakeProfit` | 止盈距离（以价格跳动为单位），0 表示不启用。 | `100` |
| `CloseAfter` | 持仓最长允许时间，超时后强制平仓。 | `15000` 秒 |
| `NoTradeStartHour` | 禁止交易区间的开始小时（0–23）。 | `22` |
| `NoTradeEndHour` | 禁止交易区间的结束小时（0–23）。 | `2` |
| `BrokerTimeOffset` | 经纪商服务器与 UTC 的小时偏移量。 | `0` |

## 转换说明
- MT4 中 iMAX3alert1 的双色缓冲被快/慢 EMA 交叉逻辑替代，仍旧在发生交叉的当根触发信号。
- Moving Averages2 指标以 EMA 模式运行，默认长度为 13，本策略同样使用该默认值。
- 仿照原脚本的持仓处理流程：先检查时间限制再评估新信号，未额外加入追踪止损等机制。

## 使用建议
1. 启动策略前先绑定目标证券并确认 `CandleType`。
2. 根据品种波动性调节 `TradeVolume`、`StopLoss`、`TakeProfit` 与 `CloseAfter`。
3. 在迁移至其他市场时，可通过优化 `FastTrendLength` / `SlowTrendLength` 来拟合原始 iMAX 行为。
4. 多实例运行时建议结合组合级别的风险控制（风控守护、交易时段管理等）。
