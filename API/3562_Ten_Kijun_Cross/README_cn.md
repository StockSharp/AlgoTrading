# TenKijun 交叉提醒策略 (ID 3562)

## 概述

该策略基于 MetaTrader 专家顾问 **TenKijun.mq4**，使用 StockSharp 高级 API 重新实现。原始 EA 只在一目均衡表的转折线（Tenkan-sen）与基准线（Kijun-sen）发生交叉时发送推送通知，不会下单。本 C# 版本保持“提醒而非交易”的设计，同时加入了 StockSharp 的订阅、图表和参数系统，便于在 Designer/Optimizer 中使用。

策略在可配置周期的收盘 K 线处工作。当有新 K 线在设定的交易时段内收盘时，利用经典的 9/26/52 参数计算一目均衡表指标，并记录最新的转折线与基准线值：

- 如果转折线自下而上穿越基准线，记录一条看涨交叉的日志信息；
- 如果转折线自上而下跌破基准线，记录一条看跌交叉的日志信息；
- 不执行买卖操作，方便用于信号提醒或与外部自动化结合。

## 指标与数据流程

- **指标**：使用 StockSharp 的 `Ichimoku` 指标，可分别设置 Tenkan、Kijun 与 Senkou Span B 的周期，保持与原始脚本一致。
- **数据订阅**：通过 `SubscribeCandles` 订阅蜡烛图，默认采用 30 分钟周期，可改成任意 `TimeSpan` 周期。
- **绑定方式**：使用 `BindEx` 获取强类型的 `IchimokuValue`，无需调用 `GetValue` 系列方法，符合项目的编码规范。
- **图表展示**：若有图表区域可用，会自动绘制蜡烛图与一目均衡表曲线，便于直观验证提醒。

## 交易时段过滤

原始脚本允许设置允许提醒的小时区间。移植版本通过两个参数实现同样的控制：

- `StartHour`：交易时段的开始小时（含），默认 0。
- `LastHour`：交易时段的结束小时（含），默认 20。

若 `StartHour` ≤ `LastHour`，提醒只在该时间区间内触发；若 `StartHour` > `LastHour`，则视为跨夜区间（例如 20 → 6 表示晚间到次日凌晨）。

## 参数说明

| 参数 | 描述 | 默认值 | 备注 |
|------|------|--------|------|
| `StartHour` | 允许提醒的起始小时 | 0 | 0-23 之间的整数 |
| `LastHour` | 允许提醒的结束小时 | 20 | 0-23 之间的整数 |
| `TenkanPeriod` | 转折线回看长度 | 9 | 支持优化 |
| `KijunPeriod` | 基准线回看长度 | 26 | 支持优化 |
| `SenkouSpanBPeriod` | 领先线 B 回看长度 | 52 | 为完整起见提供，提醒逻辑未使用云图 |
| `CandleType` | 指标使用的蜡烛类型 | 30 分钟 K 线 | 可换成任意时间框架 |

## 提醒逻辑

1. 等待首根完成的蜡烛，用于初始化上一根 Tenkan/Kijun 值。
2. 每当有新的蜡烛在交易时段内收盘时：
   - 从 `IchimokuValue` 中提取 Tenkan 与 Kijun。
   - 若上一根 Tenkan ≤ 上一根 Kijun 且当前 Tenkan > 当前 Kijun，则识别为看涨交叉并写日志。
   - 若上一根 Tenkan ≥ 上一根 Kijun 且当前 Tenkan < 当前 Kijun，则识别为看跌交叉并写日志。
   - 更新保存的上一根指标数值，等待下一次比较。

## 使用建议

- 可订阅策略日志或扩展 `ProcessCandle`，把提示转发到邮件、声音或即时通讯服务。
- 如需自动下单，可在该类基础上派生子类，在交叉发生时调用 `BuyMarket` / `SellMarket` 或其他下单方法。
- 请将时间框架调整为与 MetaTrader 图表一致，以便收到相同的交叉提醒。

## 与原始 EA 的差异

- 使用 StockSharp 的日志体系代替 MetaTrader 的 `SendNotification`，功能等效。
- 提供完整的参数元数据（显示名称、范围、可优化标记），便于在图形界面中配置。
- 自动把蜡烛和指标绘制到 StockSharp 图表，无需额外脚本。

## 文件列表

- `CS/TenKijunCrossStrategy.cs` – 策略的 C# 实现。

