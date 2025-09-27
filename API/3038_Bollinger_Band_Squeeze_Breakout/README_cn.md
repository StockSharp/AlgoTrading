# Bollinger Band Squeeze Breakout 策略

## 概述
该策略将 MetaTrader 4 中的 “BOLINGER BAND SQUEEZE” 智能交易系统完整移植到 StockSharp 高级 API。策略在主周期上识别布林带收缩阶段，并在带宽重新扩张且多重滤波确认之后开仓。本实现保留了原脚本中的多周期动量确认，同时使用 StockSharp 的订单保护和参数系统。

## 交易逻辑
1. **带宽挤压与扩张**
   - 在工作周期上计算布林带（默认长度 20、标准差 2）。
   - 比较最近一根收盘 K 线的带宽与 `RetraceCandles` 根之前的带宽。
   - 当带宽比值大于 `SqueezeRatio` 时认为出现有效突破，说明价格正从挤压区向外扩张。
2. **趋势过滤**
   - 以典型价 (H+L+C)/3 驱动的两条加权移动平均线（默认 6 与 85）判断趋势方向。做多要求快线高于慢线，做空则相反。
3. **动量确认**
   - 在更高周期上计算长度为 14 的 Momentum 指标，记录最近三次收盘的与 100 的偏离度。
   - 偏离度的最大值必须超过方向对应的阈值 (`MomentumBuyThreshold` 或 `MomentumSellThreshold`) 才允许入场。
   - 更高周期按照原 MT4 脚本的映射自动选择：例如 M15→H1、H1→D1、D1→月线；周线同样回落到月线。若无法取得高周期数据，则跳过该过滤条件。
4. **长期趋势过滤**
   - 在月线（30 天近似）上计算 MACD(12,26,9)，仅当 MACD 主线在信号线上方（做多）或下方（做空）时才允许下单，以保证宏观方向一致。
5. **入场条件**
   - 多头：满足带宽扩张、快 WMA 在慢 WMA 之上、月线 MACD 多头、动量偏离超过阈值，以及两根前后 K 线的高低点存在交叠 (`candle[-2].Low < candle[-1].High`)。
   - 空头：满足带宽扩张、快 WMA 在慢 WMA 之下、月线 MACD 空头、动量偏离超过阈值，以及对应的镜像结构 (`candle[-1].Low < candle[-2].High`)。
6. **出场条件**
   - 仓位在价格收于布林带外侧时平仓：多头触及上轨、空头触及下轨，与原始 EA 中的逻辑保持一致。
   - 通过 `StartProtection()` 启动 StockSharp 的保护单基础设施，方便后续在设计器中追加止损/止盈模块。

## 指标与数据订阅
- 主周期 K 线 (`CandleType`)。
- 按映射获得的高周期 K 线用于 Momentum 过滤。
- 月线（30 天近似）用于 MACD 长周期过滤。
- 使用的指标：BollingerBands、WeightedMovingAverage（两条）、Momentum、MovingAverageConvergenceDivergenceSignal。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 15 分钟 | 工作周期。 |
| `BollingerPeriod` | 20 | 布林带长度。 |
| `BollingerWidth` | 2.0 | 布林带标准差倍数。 |
| `SqueezeRatio` | 1.1 | 当前带宽与历史带宽的最小扩张比例。 |
| `RetraceCandles` | 10 | 用于比较挤压的历史 K 线数量。 |
| `FastMaLength` | 6 | 快速加权均线长度（典型价）。 |
| `SlowMaLength` | 85 | 慢速加权均线长度（典型价）。 |
| `MomentumLength` | 14 | 高周期 Momentum 指标长度。 |
| `MomentumBuyThreshold` | 0.3 | 做多所需的最小动量偏离度。 |
| `MomentumSellThreshold` | 0.3 | 做空所需的最小动量偏离度。 |

所有参数都通过 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中优化或运行时动态调整。

## 实现说明
- 使用 `SubscribeCandles().BindEx(...)` 绑定指标，遵循高层 API “不向 `Strategy.Indicators` 手动添加对象”的约束。
- 加权均线在烛线回调中以典型价驱动，保持与 MT4 中 LWMA 计算一致。
- 高周期 Momentum 仅保留最近三次偏离度，模拟原脚本中 `iMomentum` 1–3 的引用。
- 月线 MACD 的主线与信号值存储在字段中，确保主周期每个信号都能引用最新宏观方向。
- 平仓逻辑采用触达布林带外轨的条件，替代原 EA 中复杂的拖尾止损与保本模块，但保留“触带即出”的含义。
- 下单量使用基础的 `Strategy.Volume`，在反向开仓时会自动加上 `Math.Abs(Position)` 以覆盖旧仓位。
