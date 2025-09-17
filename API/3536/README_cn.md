# 新K线策略

## 概述

该示例演示如何使用 StockSharp 的高级蜡烛订阅 API 来侦测新的K线事件。它是对 MQL 专家顾问 `NewBar.mq5` 的移植，原策略展示了当图表上出现新K线时应如何响应。

策略不会发出真实交易，而是通过日志输出以下信息：

- 策略启动后接收到的首个更新（对应 MQL 中 `dtBarPrevious == WRONG_VALUE` 的情况）。
- 每根后续K线的第一笔报价。
- 当前K线形成过程中到达的额外报价。
- 当K线收盘时。

## 核心逻辑

1. 通过 `SubscribeCandles` 订阅配置的蜡烛序列，并绑定 `ProcessCandle` 处理函数。
2. 追踪当前K线的开盘时间，一旦时间发生变化就说明新K线已经开始。
3. 第一次接收到数据时调用 `HandleFirstObservation`，模拟在K线中途附加顾问的情况。
4. 每当一根新K线开始时调用 `HandleNewBar`，可在此加入发单或信号逻辑。
5. 在K线处于活动状态时调用 `HandleSameBarTick`，以便执行逐笔处理或风控。
6. 当收到 `CandleStates.Finished` 状态时执行 `HandleBarClosed`。

每个辅助方法都带有英文注释，方便在此模板上继续扩展自定义行为。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于侦测新K线事件的蜡烛类型。 | 1分钟周期 |

## 如何扩展模板

- 在 `HandleNewBar` 中加入进场逻辑，可确保每根K线只触发一次。
- 在 `HandleSameBarTick` 中实现盘中检查或风险管理。
- 在 `HandleBarClosed` 中处理平仓或移动止损。

## 与原始 MQL 版本的差异

- 使用 StockSharp 的高级蜡烛订阅，避免手动调用 `iTime` 轮询时间。
- 提供与原脚本注释块对应的辅助方法，明确指出可插入自定义规则的位置。
- 通过框架自带的 `Log` 工具输出结构化日志，而非在代码中直接写注释。
