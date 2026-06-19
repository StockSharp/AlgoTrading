# Rubberbands Safety Net 策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader 平台上的 RUBBERBANDS 1.6 专家顾问移植到 StockSharp 高级 API。原始版本始终持有一多一空两笔单，并在获利后立即替换该方向，同时在浮亏达到指定金额时启动安全加仓网格。由于 StockSharp 采用净持仓模型，本移植版保留交替循环，但在安全模式下改为沿当前方向进行加仓，从而在保持原有现金阈值的同时符合净头寸逻辑。

## 交易逻辑

- **循环启动：** 每分钟结束时或启用 `Enter Now` 开关时，按 `BaseVolume` 开立市价单。下一轮循环自动切换方向（多→空→多）。
- **基础盈利目标：** 将实时浮动盈亏与 `TargetProfitPerLot * BaseVolume` 比较。达到目标后立即平仓，并在下一轮切换方向。
- **会话控制：** `UseSessionTakeProfit` 与 `UseSessionStopLoss` 依据基础手数监控“已实现 + 未实现”的货币收益。触发任一阈值时立即清仓并重置统计。
- **安全模式：** 启用后，当浮亏大于 `SafetyStartPerLot * BaseVolume` 时进入安全模式，并按 `SafetyVolume` 沿当前方向追加仓位。每再亏损 `SafetyStepPerLot`（按安全手数计算）会自动计划新的加仓。
- **安全退出：** 安全模式下，当未实现收益达到 `SafetyProfitPerLot * |Position|`，或会话利润超过 `SafetyModeTakeProfitPerLot * BaseVolume` 时平掉整笔头寸。

## 入场条件

### 做多
- 当前无持仓，且分钟刚结束或手动打开 `Enter Now`。
- 当前循环方向为多头。
- `Stop Trading` 开关关闭。

### 做空
- 条件同做多，但循环方向为做空。

## 出场管理

- **基础目标达成：** 平掉现有仓位并切换下个循环方向。
- **会话 TP/SL：** 触发任一阈值后平仓并清零利润计数，等待下一轮触发。
- **安全盈利：** 安全模式下，当净盈亏达到目标时平仓退出。
- **安全加仓：** 每当浮亏按 `SafetyStepPerLot` 递增时追加一笔安全单。
- **手动退出：** `Close Now` 打开后将在下一根 K 线上市价平仓并清零累计利润。

## 参数

| 参数 | 说明 |
|------|------|
| `BaseVolume` | 初始市价单的数量。 |
| `TargetProfitPerLot` | 基础仓位的单手盈利目标（货币值）。 |
| `UseSessionTakeProfit` / `SessionTakeProfitPerLot` | 启用并设置会话级别的止盈。 |
| `UseSessionStopLoss` / `SessionStopLossPerLot` | 启用并设置会话级别的止损。 |
| `UseSafetyMode` | 是否启用安全加仓机制。 |
| `SafetyStartPerLot` | 触发安全模式的浮亏阈值（按基础手数）。 |
| `SafetyVolume` | 每次安全加仓的手数。 |
| `SafetyStepPerLot` | 追加下一笔安全单所需的额外浮亏（按安全手数）。 |
| `SafetyProfitPerLot` | 安全模式下的盈利目标。 |
| `SafetyModeTakeProfitPerLot` | 安全模式激活时的会话级盈利目标。 |
| `UseInitialState` 及相关参数 | 允许在重启时恢复利润与安全模式状态。 |
| `QuiesceNow`, `Enter Now`, `Stop Trading`, `Close Now` | 与原版 extern 变量对应的手动开关。 |
| `CandleType` | 驱动主循环的蜡烛时间框架（默认 1 分钟）。 |

## 使用说明

- StockSharp 只能维护单一净持仓，因此本策略在安全模式下通过同向加仓来模拟原策略的对冲网格。所有金额阈值仍以账户货币计量，需按交易品种的最小变动值调整。
- 手动开关可在界面上随时调整，用于暂停入场、立即入场或强制平仓。
- 启动时会调用 `StartProtection()`，以便复用 StockSharp 的标准保护机制。
- 加仓函数会按照 `VolumeStep`、`VolumeMin`、`VolumeMax` 自动对齐数量，请确保这些合约参数配置正确。
