# BuySellOnYourPrice 策略

## 概述
- 将 MetaTrader 专家顾问 **BuySellonYourPrice.mq5**（id 35391）迁移到 StockSharp 高级 API。
- 启动时仅发送一次订单，复现原策略在没有挂单与仓位时才下单的限制。
- 支持市价、限价与止损订单，并接受以绝对价格表示的止损 / 止盈。
- 当能够根据给定价格计算出有效距离时，会自动通过 `StartProtection` 配置保护性委托。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Mode` | 下单类型（None、Buy、Sell、BuyLimit、SellLimit、BuyStop、SellStop）。 | `None` |
| `OrderVolume` | 发送订单的数量。 | `1` |
| `EntryPrice` | 挂单使用的价格；市价单忽略。 | `0` |
| `StopLossPrice` | 绝对止损价格。 | `0` |
| `TakeProfitPrice` | 绝对止盈价格。 | `0` |

## 交易流程
1. 启动时逐项检查：
   - `Mode` 不能为 `None`。
   - `OrderVolume` 必须为正数。
   - 当前没有持仓且没有活动订单（对应 MQL 中 `OrdersTotal()==0` 与 `PositionsTotal()==0`）。
2. 解析入场价格：
   - 市价模式优先采用当前最优买卖价，若缺失则回退到最新价或 `EntryPrice`。
   - 挂单模式要求 `EntryPrice > 0`。
3. 根据止损与止盈价格计算保护性距离，仅在得到正值时调用 `StartProtection`。
4. 按照所选模式调用 `BuyMarket`、`SellLimit`、`BuyStop` 等方法，并通过日志提示操作结果。

## 与原版差异
- 使用 `AddInfoLog` 记录消息，代替 MQL 的 `Print`。
- 只有在能够计算出正向距离时才注册保护性订单，避免无效设置。
- 市价委托依赖 Level1 数据，若尚未收到行情则不会立即下单。

## 使用建议
- 在启动前绑定目标交易品种，并确保能够接收 Level1 行情，用于市价单。
- 对于挂单，请提前填写 `EntryPrice`、`StopLossPrice`、`TakeProfitPrice`，并采用绝对价格。
- 将 `Mode` 设为 `None` 可在不移除策略的情况下暂停交易。
