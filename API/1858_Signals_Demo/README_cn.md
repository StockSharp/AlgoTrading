# Signals Demo 策略
[English](README.md) | [Русский](README_ru.md)

示例策略，用于演示如何订阅和取消订阅外部交易信号。
在订阅之前，策略会检查并调整基本风险参数。

## 细节

- **目的**：展示在 StockSharp 中处理信号订阅的流程。
- **交易**：策略不执行交易，仅记录订阅操作。
- **参数**：
  - `SignalId` – 要跟随的信号标识。
  - `EquityLimit` – 复制信号时可使用的最大资金。
  - `Slippage` – 复制交易时允许的滑点。
  - `DepositPercent` – 分配给该信号的账户资金百分比。
- **默认值**：
  - `SignalId` = 0
  - `EquityLimit` = 0
  - `Slippage` = 2
  - `DepositPercent` = 5
- **验证**：
  - `DepositPercent` 被限制在 5–95 范围内。
  - 负值的 `EquityLimit` 和 `Slippage` 将被重置为 0。

## 使用方法

1. 在界面中配置参数。
2. 启动策略后，它会验证参数并记录订阅行为。
3. 停止策略以取消订阅。

该示例仅用于学习，不包含真实的交易逻辑。
