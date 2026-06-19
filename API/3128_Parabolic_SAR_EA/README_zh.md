# 抛物线SAR EA策略

## 概览
**抛物线SAR EA策略** 是对 `MQL/23039` 中 MetaTrader 专家顾问 `Parabolic SAR EA.mq5` 的 StockSharp 高层 API 移植版本。原始脚本在指定周期上追踪抛物线SAR指标的翻转，在出现条件时以市价开仓，并按照MetaTrader的“点”定义（包含三位和五位报价的小数调整）设置固定的止损和止盈。C# 版本通过蜡烛图订阅与 `ParabolicSar` 指标绑定，完整复刻了逐K线的决策流程，同时遵循项目的编码规范。

## 交易逻辑
1. **数据准备**
   - 策略订阅用户配置的K线类型（默认30分钟），并以可调节的加速步长和最大值初始化抛物线SAR指标。
   - 指标值通过高层 `Bind` 回调在每根K线上推送给策略。
2. **信号判定**
   - 多头信号：已收盘K线的抛物线SAR数值严格低于该K线最低价。
   - 空头信号：已收盘K线的抛物线SAR数值严格高于该K线最高价。
   - 仅在 `CandleStates.Finished` 的完成K线上评估条件，以匹配MQL中“新柱”触发的行为。
3. **持仓切换**
   - 若当前存在反向仓位，会在入场前通过将市价单数量加上净持仓量的绝对值来一次性平仓并反手，相当于MetaTrader中的 `ClosePosition` + `OpenPosition` 组合。
   - 每次开仓都会重新计算止损与止盈价格，并根据报价小数位自动套用 `PriceStep × 10` 的点值换算规则。
4. **保护性退出**
   - 每根完成的K线都会检查最高价/最低价是否触及保存的止损或止盈水平，一旦触发即以市价平仓并清除对应目标。
   - 保护逻辑优先于同一根K线上的新信号，模拟原始EA在经纪端挂出的止损/止盈触发顺序。

## 指标与数据说明
- 使用StockSharp内置的 `ParabolicSar` 指标（参数 `SarStep` 与 `SarMaximum`）。
- 通过 `SubscribeCandles` 完成K线订阅，不会把指标添加到 `Strategy.Indicators`，符合项目指南。
- 只有在 `IsFormedAndOnlineAndAllowTrading()` 返回真时才会进行交易，确保连接器允许下单且行情已就绪。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `TradeVolume` | `1` | 市价单手数，修改时同步更新 `Strategy.Volume`。|
| `StopLossPips` | `50` | 以MetaTrader点数表示的止损距离。对于3位或5位小数报价，1点= `PriceStep × 10`，否则为 `PriceStep`。填 `0` 可关闭止损。|
| `TakeProfitPips` | `50` | 以MetaTrader点数表示的止盈距离，换算规则与止损一致。填 `0` 可关闭止盈。|
| `SarStep` | `0.02` | 抛物线SAR的加速步长。|
| `SarMaximum` | `0.2` | 抛物线SAR的最大加速值。|
| `CandleType` | `30分钟周期` | 用于计算的K线类型，支持任意基于 `TimeFrame` 的 `DataType`。|

## 风险管理与行为
- 止损/止盈在每次成交后重新计算并保存在策略内部，不会向交易所登记挂单。
- 若同一根K线同时触及止损与止盈，先执行止损检查，遵循原EA的保守处理方式。
- 当连接器未提供有效的 `PriceStep` 时，策略会退回到 `0.0001` 以避免零距离保护价位。
- 策略仅维护单一净持仓，不做加仓或金字塔式操作，抛物线SAR翻越价格时直接反手。

## 移植细节
- MetaTrader 中的 `InpBarCurrent` 为1，即使用上一根已完成的K线做判断。移植版通过仅处理 `Finished` 状态的K线达到同样效果。
- 原专家顾问通过 `CheckVolumeValue` 检测手数与经纪商限制。StockSharp 交由连接器验证，但 `TradeVolume` 仍要求正数。
- 按照任务要求，本策略暂未提供 Python 版本。
