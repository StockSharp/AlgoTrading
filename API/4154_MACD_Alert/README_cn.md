# MACD Alert 策略

## 概述
本策略完整复刻了 MetaTrader 专家顾问 *MACD_Alert.mq4* 的行为。原始脚本在 5 分钟周期上计算 MACD 主线（12/26 EMA，9 期信号线），当主线数值高于 `0.00060` 或低于 `-0.00060` 时调用 `Alert()`。StockSharp 版本订阅同样的蜡烛序列，创建参数一致的 `MovingAverageConvergenceDivergence` 指标，并在每次触发阈值时向策略日志写入提示消息。

模块仅提供信息提醒，不会自动下单或管理仓位，非常适合希望在动量达到极值时收到提示的人工交易者。

## 转换亮点
- 采用 `SubscribeCandles` 与 `Bind` 的高阶 API，避免手动缓存数据，指标直接得到聚合好的蜡烛。
- 只处理收盘完成的蜡烛，与原版 EA 在每根 K 线收盘时检查条件的方式一致。
- 默认参数（MACD 12/26/9、±0.00060 阈值、M5 周期）全部保留，同时暴露为可优化的策略参数，方便微调。
- 通过 `AddInfoLog` 输出提醒，兼容 StockSharp 的日志/通知体系。
- 若宿主环境支持绘图，会在图表上显示蜡烛与 MACD 曲线，便于人工确认。

## 参数
| 参数 | 类型 | 说明 |
| --- | --- | --- |
| `MacdFastPeriod` | `int` | MACD 快速 EMA 周期，默认 `12`。 |
| `MacdSlowPeriod` | `int` | MACD 慢速 EMA 周期，默认 `26`。 |
| `MacdSignalPeriod` | `int` | MACD 信号线平滑周期，默认 `9`。 |
| `UpperThreshold` | `decimal` | 触发多头提醒的 MACD 数值，默认 `0.00060`。 |
| `LowerThreshold` | `decimal` | 触发空头提醒的 MACD 数值，默认 `-0.00060`。 |
| `EnableAlerts` | `bool` | 是否写入提醒消息。 |
| `CandleType` | `DataType` | 用于计算 MACD 的蜡烛类型，默认 5 分钟蜡烛。 |

## 提醒逻辑
1. `OnStarted` 中创建 `MovingAverageConvergenceDivergence` 指标并绑定到蜡烛订阅。
2. 每当蜡烛收盘时读取 MACD 主线。
3. 数值 ≥ `UpperThreshold` 时写入多头提醒消息。
4. 数值 ≤ `LowerThreshold` 时写入空头提醒消息。
5. 指标同时输出信号线和柱状图，当前实现保持计算但不使用，以保持与原版一致。

由于在每根收盘蜡烛上仅产生一次提示，可以避免在高频报价下的重复刷屏，同时保持与 MetaTrader 提醒相同的触发条件。

## 使用步骤
1. 选择要监控的品种，并设置 `CandleType`（默认 5 分钟）。
2. 根据需要调整 MACD 周期或阈值。
3. 启动策略。指标形成后，只要突破设定阈值就会在日志中生成提示。
4. 若需声音或推送提醒，可将日志对接到外部通知系统。

## 提示示例
```
MACD main line 0.00074 exceeded the upper threshold 0.00060 at 2024-05-13 09:25:00.
MACD main line -0.00068 fell below the lower threshold -0.00060 at 2024-05-13 11:40:00.
```
这些文本与原 EA 的 `Alert()` 消息完全对应。

## 可扩展性
- 在 `ProcessCandle` 中加入下单逻辑，即可把提醒改造成自动交易策略。
- 通过外部控制器动态修改阈值，例如使用 ATR、标准差等波动率指标自适应设定。
- 切换 `CandleType` 到更高或更低的时间框架，以适应不同节奏的监控需求。
- 将日志结果写入数据库或发送到团队协作工具，实现跨平台共享。
