# IBS RSI CCI v4 X2 策略

## 概述

**IBS RSI CCI v4 X2 策略** 是一个多时间框架的动量系统，使用内部柱强度 (IBS)、相对强弱指数 (RSI) 与商品通道指数 (CCI) 的组合。原始的 MetaTrader 5 指标被移植到 StockSharp 环境，并通过高级 K 线订阅完成信号绑定。策略包含两组独立的指标管线：

* **趋势时间框架**（默认 8 小时）确定多空方向；
* **信号时间框架**（默认 1 小时）生成具体的入场与出场指令。

在每个时间框架内都构建一个复合震荡指标。震荡值由 IBS、RSI 与 CCI 的加权和组成，并通过阈值进行限制，再配合包络线进行平滑。复合值与包络线的交叉就是交易信号的核心依据。

## 交易规则

1. **趋势识别**：如果慢周期的复合值位于包络线上方，策略视为多头趋势；反之视为空头趋势。
2. **信号确认**：快周期连续读取两根已完成 K 线的复合值与包络值，只有在上一根与当前一根形成有效交叉时才认为信号成立。
3. **开仓条件**：
   * 当允许做多且趋势为多头时，若复合值从包络线上方跌破后再次向下穿越包络线（保持与原始指标一致的方向），触发买入；
   * 当允许做空且趋势为空头时，若复合值从包络线下方向上穿越包络线，触发卖出。
4. **平仓条件**：
   * `_CloseLongOnSignalCross` 或 `_CloseShortOnSignalCross` 为真时，在快周期的反向交叉立即平仓；
   * `_CloseLongOnTrendFlip` 或 `_CloseShortOnTrendFlip` 为真时，趋势反转后立即离场；
   * 使用 `StartProtection` 将点数形式的止损与止盈转换为绝对价格距离（基于合约的 `PriceStep`）。

## 参数说明

| 参数 | 说明 |
| --- | --- |
| `OrderVolume` | 开仓基础手数，翻仓时会自动叠加现有仓位。 |
| `TrendCandleType` / `SignalCandleType` | 慢/快时间框架的 K 线类型。 |
| `TrendIbsPeriod`, `SignalIbsPeriod` | IBS 平滑周期。 |
| `TrendIbsMaType`, `SignalIbsMaType` | IBS 平滑的均线类型（简单、指数、平滑、加权）。 |
| `TrendRsiPeriod`, `SignalRsiPeriod` | RSI 周期。 |
| `TrendRsiPrice`, `SignalRsiPrice` | RSI 使用的价格类型。 |
| `TrendCciPeriod`, `SignalCciPeriod` | CCI 周期。 |
| `TrendCciPrice`, `SignalCciPrice` | CCI 使用的价格类型。 |
| `TrendThreshold`, `SignalThreshold` | 复合震荡值的限制阈值，控制敏感度。 |
| `TrendRangePeriod`, `TrendSmoothPeriod` | 慢周期包络线的窗口长度与平滑长度。 |
| `SignalRangePeriod`, `SignalSmoothPeriod` | 快周期包络线的窗口长度与平滑长度。 |
| `TrendSignalBar`, `SignalSignalBar` | 读取历史值时向后偏移的 K 线数量。 |
| `AllowLongEntries`, `AllowShortEntries` | 是否允许新的多单或空单。 |
| `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip` | 趋势反转时是否强制离场。 |
| `CloseLongOnSignalCross`, `CloseShortOnSignalCross` | 快周期交叉是否直接平仓。 |
| `StopLossPoints`, `TakeProfitPoints` | 以价格步长为单位的止损与止盈距离。 |

## 使用建议

1. 在启动前配置好交易品种以及两个时间框架；`GetWorkingSecurities` 会自动完成订阅。
2. 默认参数与原始 MQ5 脚本一致，适合作为对照测试基础。
3. 若市场波动剧烈，可调整阈值与包络窗口参数以获得更快的响应；数值越小信号越敏感。
4. 自定义的 CCI 计算依赖连续的 K 线数据，如数据缺失请谨慎使用或进行预处理。
5. 策略会在可用的图表区域绘制信号周期的 K 线及成交记录，方便可视化调试。

## 风险提示

* 该策略不包含资金管理或交易时段过滤，请根据实际需求补充。
* 价格步长为空或为零时止盈止损将失效，需要在代码中提供合适的备用值。
* 入场采取反向手数覆盖方式，若经纪商不允许同一订单同时平仓与反向开仓，需要改写下单逻辑。

## 拓展思路

* 独立调整两个时间框架的指标参数，探索不同周期组合；
* 将复合震荡指标绘制到自定义图层，观察阈值对信号的影响；
* 可以增加滤波器（如成交量、时间、波动率）以提高稳定性；
* 若需要外部优化，可通过 StockSharp 的参数系统直接导出并运行优化流程。
