# Visual Trader Simulator Edition

该策略是 MetaTrader 平台 VisualTrader 脚本的简化移植版本。

策略在启动时根据参数选择买入或卖出，并使用指定的止盈和止损（绝对价格）进行保护。此示例展示了如何使用 StockSharp 高层 API 重建手动交易管理脚本。

## 参数

- **Trade Direction** – 交易方向，Buy 或 Sell。
- **Take Profit** – 可选的止盈价格，绝对值；为 0 时禁用。
- **Stop Loss** – 可选的止损价格，绝对值；为 0 时禁用。
- **Volume** – 市价单的数量。

## 交易逻辑

启动时策略执行以下步骤：

1. 通过 `StartProtection` 创建止盈止损。
2. 按选择的方向发送市价单。

该示例不依赖任何指标或行情数据，仅用于演示目的。
