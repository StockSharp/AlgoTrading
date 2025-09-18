# 符号同步策略

## 概述
**Symbol Sync Strategy** 在 StockSharp 平台中复刻 MetaTrader 工具 `SymbolSyncEA` 的行为。策略会监控主策略当前使用的品种，并把该证券同步到所有已注册的从属策略，使整个工作空间始终聚焦同一个标的。

## 核心思路
- 启动时保存初始证券，作为后续恢复的备用值。
- 维护一个可配置的同步列表，列表中的策略都会跟随主策略的证券。
- 允许通过直接赋值 `Security` 或者提供证券标识两种方式触发切换。
- 提供手动同步与恢复到初始证券的方法，最大程度地还原原始 EA 的功能。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `ChartLimit` | 允许同步的策略最大数量，用于防止意外的大规模变更。 | `10` |
| `SyncSecurityId` | 将要传播的证券标识，留空时自动使用当前策略的证券。 | `""` |

## 公共方法
- `RegisterLinkedStrategy(Strategy strategy)`：把策略加入同步列表，成功返回 `true`。
- `UnregisterLinkedStrategy(Strategy strategy)`：从列表中移除指定策略。
- `ChangeSyncSecurity(Security security)`：使用给定的 `Security` 实例并同步所有从属策略。
- `ChangeSyncSecurity(string securityId)`：通过 `SecurityProvider` 查找标识并调用上一方法。
- `ResetToInitialSecurity()`：恢复为启动时保存的证券。
- `SyncSymbols()`：在不修改标识的情况下强制执行一次同步。

## 使用流程
1. 创建 `SymbolSyncStrategy`，在启动前设置主策略的 `Security` 或 `SyncSecurityId`。
2. 调用 `RegisterLinkedStrategy` 注册所有需要跟随的子策略（例如不同的时间周期、统计模块等）。
3. 当需要切换主品种时，调用 `ChangeSyncSecurity(Security)` 或 `ChangeSyncSecurity(string)`。
4. 如果外部组件可能修改过子策略，可调用 `SyncSymbols()` 手动强制同步。

## 与 MQL 版本的差异
- 针对 StockSharp 的 `Strategy` 实例，而非 MetaTrader 的图表窗口。
- 通过 `SecurityProvider` 查找证券标识。
- 增加了防御性日志和同步数量上限。
- 提供显式的重置与手动同步方法，便于自动化流程集成。

## 备注
- 此策略不发送交易指令，定位为基础设施辅助组件。
- 代码中的注释统一为英文，以符合项目规范。
