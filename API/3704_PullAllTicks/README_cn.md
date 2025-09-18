# Pull All Ticks 策略

## 概述

**Pull All Ticks Strategy** 是 MetaTrader 脚本 `pull all ticks.mq5`（ID 56324）的 StockSharp 移植版本。原始脚本会在经纪商处查找最早的可用 tick，并按批量下载整个历史，同时把进度写入磁盘。该 C# 策略使用 StockSharp 的高级订阅 API 复刻这一流程：它持续接收 tick，记录最早和最新的时间戳，并将进度保存在状态文件中，以便重启后继续下载。

策略本身不执行交易，仅作为数据采集工具。它订阅所选 `Security` 的逐笔成交流，定期刷新状态文件，并在达到设定的日期下限时自动停止。

## 工作流程

1. 启动时根据 `ManagerFolder` 与 `StatusFileName` 参数计算状态文件路径。
2. 如果发现已有文件，则恢复保存的时间戳、tick 计数以及已完成的数据包数量。
3. 通过 `SubscribeTrades().Bind(ProcessTrade).Start()` 订阅逐笔成交，确保所有 tick 都按时间顺序处理。
4. 每个 tick 都会更新最早/最新时间戳、增加 tick 计数，并在累计到一个完整数据包（由 `TickPacketSize` 定义）时触发保存。
5. 进度以键值对文本格式写入磁盘，同时通过 `LogInfo` 输出，作用类似 MetaTrader 中的 `Comment`。
6. 当最早的 tick 时间早于 `OldestLimit`（且已启用 `LimitDate`）时，策略会自动停止。

## 参数

| 参数 | 说明 |
|------|------|
| `LimitDate` | 是否在达到最早日期后停止，对应 MQL 输入 `limitDate`。 |
| `OldestLimit` | 历史扫描的最小时间界限。达到后立即停止。 |
| `TickPacketSize` | 每处理多少个 tick 就持久化一次，等价于原脚本的 `tick_packets`。 |
| `RequestDelay` | 两次状态更新之间的最小间隔，用于替代源码中的 `Sleep(44)`。 |
| `ManagerFolder` | 存放进度文件的目录，对应 `MANAGER_FOLDER`。 |
| `StatusFileName` | 进度文件的名称，对应 `MANAGER_STATUS_FILE`。 |

全部参数都通过 `StrategyParam<T>` 暴露，可在 Designer、Shell 或 Runner 中调整或用于优化。

## 使用方法

1. 启动前为策略设置目标 `Security`。
2. 按需求调整参数，例如指定存储目录或自定义日期限制。
3. 启动策略：若状态文件存在，将会先恢复记录，然后在处理 tick 的同时定期更新文件。
4. 任何时候都可以手动停止；停止时会立即写入进度，方便下次继续。

## 与 MetaTrader 版本的差异

- 使用 tick 订阅事件驱动替代 `OnTimer` 轮询，不再依赖显式循环。
- 进度文件采用 UTF-8 文本格式，便于查看和调试，而非原脚本的二进制写法。
- 通过可配置的 `RequestDelay` 节流更新频率，无需调用 `Sleep`。
- 文件操作全部由 .NET API（如 `Directory.CreateDirectory`、`File.WriteAllText`）完成。

## 前置条件

- StockSharp 连接需能提供目标品种的 tick 数据。
- `ManagerFolder` 所指路径需要写入权限。

满足这些条件后，策略即可在 StockSharp 体系内还原原始脚本的 tick 批量下载流程。
