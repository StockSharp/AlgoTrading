# Syndicate Trader 策略
[English](README.md) | [Русский](README_ru.md)

该策略是将 `MQL/12351` 文件夹中的原始 MetaTrader 脚本 **Syndicate_Trader_v_1_04.mq4** 转换到 StockSharp 平台的结果。

策略基于快慢指数均线的交叉，并通过成交量激增进行确认。可选的时间过滤器限制策略只在指定时段运行。风险通过简单的止盈和止损来控制。

## 细节

- **入场条件**：
  - **做多**：快速 EMA 上穿慢速 EMA 且成交量大于平均成交量乘以系数。
  - **做空**：快速 EMA 下穿慢速 EMA 并满足相同的成交量条件。
- **多空方向**：双向。
- **出场条件**：
  - 反向均线交叉。
  - 触发止损或止盈。
  - 超出允许的交易时段。
- **止损止盈**：按价格点设置的固定止盈和止损。
- **过滤器**：
  - 成交量激增过滤。
  - 可选的交易时段过滤。

## 参数

| 名称 | 说明 |
|------|------|
| `FastEmaLength` | 快速 EMA 周期。 |
| `SlowEmaLength` | 慢速 EMA 周期。 |
| `VolumeMaLength` | 成交量均线周期。 |
| `VolumeMultiplier` | 用于判定成交量激增的系数。 |
| `TakeProfitPoints` | 按价格点计算的止盈。 |
| `StopLossPoints` | 按价格点计算的止损。 |
| `UseSessionFilter` | 是否启用时段过滤。 |
| `SessionStartHour/SessionStartMinute` | 交易开始时间。 |
| `SessionEndHour/SessionEndMinute` | 交易结束时间。 |
| `CandleType` | K 线类型和时间框架。 |

