# Indicator Buffers 策略

本策略演示如何使用 StockSharp 高级 API 查看指标缓冲区。

原始的 MQL 专家顾问 `indicator_buffers.mq4` 通过 iCustom 调用最多八个指标缓冲区，并在 MetaTrader 图表上以文本标签显示它们的值。该移植版本旨在展示如何在 StockSharp 中构建类似的诊断工具。

策略订阅蜡烛数据并处理 **Bollinger Bands** 指标。在每个完成的蜡烛上，它会将中轨、上轨和下轨的数值分别记录为 Buffer0、Buffer1 和 Buffer2。其余缓冲区（Buffer3–Buffer7）保留给具有更多组件的指标，并被标记为未使用。

此实现仅用于教育目的，不会发送任何交易指令。

## 参数

- **Candle Type** – 计算所使用的蜡烛类型。
- **Bands Period** – Bollinger Bands 移动平均的周期。
- **Bands Width** – Bollinger Bands 的宽度系数。

## 使用方法

1. 将策略添加到您的 StockSharp 环境。
2. 如有需要，可调整参数。
3. 启动策略，日志中会在每个完成的蜡烛上显示缓冲区的值。

## 说明

- 示例选择 Bollinger Bands，因为它提供多个输出缓冲区。
- 可以扩展处理方法以支持更多缓冲区。
