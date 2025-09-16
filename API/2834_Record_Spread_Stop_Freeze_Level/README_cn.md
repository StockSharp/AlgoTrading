# Record Spread Stop Freeze Level 策略
[Русский](README_ru.md) | [English](README.md)

Record Spread Stop Freeze Level 策略是一款用于定期记录一个或多个交易品种市场微结构指标的工具。它复刻原始 MetaTrader 专家的行为，收集价差、止损距离和冻结距离信息，并将结果保存到分号分隔的日志文件中，方便后续分析。

## 详情

- **用途**：生成价差、止损和冻结水平的时间序列，便于监控交易环境
- **数据来源**：
  - 每个监控品种的 Level1 行情（最佳买卖报价以及数据提供商的扩展字段）
  - 连接器状态，用于获取服务器时间与连接是否在线
- **记录频率**：可配置的分钟级定时器（默认 1 分钟）
- **监控品种**：
  - 可选择包含策略的主标的
  - 支持配置额外的 `Security` 列表以并行监控
- **输出内容**：
  - 保存到平台 `Logs` 目录下的 CSV 文本文件
  - 在新的会话开始前自动将旧文件备份到 `Logs/BUP` 子目录

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `RecordPeriodMinutes` | 定时器触发写入的时间间隔（分钟）。必须大于 0。 | `1` |
| `LogFilePrefix` | 生成日志文件名时使用的前缀。 | `"MktData"` |
| `IncludePrimarySecurity` | 为 `true` 时，将策略主标的加入监控列表。 | `true` |
| `AdditionalSecurities` | 需要与主标的一起记录的额外品种列表。 | 空 |

## 日志文件结构

- **路径**：`<应用程序目录>/Logs/<prefix>_Acc_<account>.csv`
  - `<应用程序目录>` 指 `AppDomain.CurrentDomain.BaseDirectory`
  - `<account>` 取自绑定的投资组合名称；如果为空则使用策略标识
- **备份**：写入前会将旧文件复制到 `Logs/BUP/<prefix>_Acc_<account>.csv`
- **表头**：
  - `TimeLocal;TimeServer;IsConnected;SYMBOL_Spread;SYMBOL_StopLevel;SYMBOL_FreezeLevel;...`
  - `SYMBOL` 由 `Security.Id` 派生并清理非法字符
- **数据行**：
  - `TimeLocal`：工作站本地时间（ISO-8601 格式，`DateTimeOffset.Now`）
  - `TimeServer`：连接器时间（`Strategy.CurrentTime`，ISO-8601 格式）
  - `IsConnected`：当连接保持在线时为 `True`，否则为 `False`
  - `Spread`：若同时存在最佳买价与卖价，则为 `BestAsk - BestBid`；缺失则写入 `N/A`
  - `StopLevel` / `FreezeLevel`：仅当数据提供商发送相应 Level1 字段时填充；否则写入 `N/A`

## 使用建议

1. 将策略绑定到能够提供所有监控品种 Level1 行情的投资组合/连接器。
2. 在 `AdditionalSecurities` 参数中添加需要额外监控的品种。
3. 根据需求调整日志前缀和记录间隔，确保频率适合运行环境。
4. 启动策略：它会订阅 Level1 数据、创建新的日志文件，并在每次定时器触发时写入一行。
5. 在 `Logs` 目录检查生成的文件，每次启动都会自动备份上一份日志。

## 限制

- 止损和冻结距离字段取决于经纪商实现。如果连接器不提供这些 Level1 信息，日志中会显示 `N/A`。
- 定时器分辨率以分钟为单位，如需更高频率需降低参数值并确认环境能够支持。
- 该策略不执行任何下单或持仓管理，仅用于诊断和数据采集。
