# TPSL Insert 策略

该策略是 MetaTrader 4 脚本 **TPSL-Insert.mq4** 的 StockSharp 版本。它不会产生任何买卖信号，仅用于给现有持仓自动添加止盈和止损。

## 工作原理

1. 启动时读取参数 `TakeProfitPips` 和 `StopLossPips`。
2. 根据合约的 `PriceStep` 将点数转换为价格。
3. 调用 `StartProtection` 下达保护性委托。
   - 如果已有持仓，则立即挂出止盈和止损。
   - 策略之后开仓的交易同样会自动受到保护。

在手动或其他系统开仓后，需要快速添加风险控制时，该策略非常方便。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|--------|
| `TakeProfitPips` | 止盈点数。 | `35` |
| `StopLossPips` | 止损点数。 | `100` |

## 备注

- 策略不订阅市场数据，也不包含进出场逻辑。
- `StartProtection` 会自动管理保护性订单的挂出与撤销。
