# Sample Check Pending Order 策略
[English](README.md) | [Русский](README_ru.md)

Sample Check Pending Order 策略始终在市场上保持一张买入止损单和一张卖出止损单。原始的 MetaTrader 5 专家顾问由 Tungman 编写，会检查经纪商是否接受指定手数、确认双向都有足够的可用保证金，然后在当前买价和卖价附近挂出新的挂单，并设置一天的有效期。本实现把同样的流程迁移到 StockSharp，高度依赖高级别下单 API 和 Level 1 行情。

## 交易逻辑

1. **行情处理**
   - 策略订阅 Level 1 数据流，缓存最新买一价与卖一价。
   - 只有在获得双向报价并且 `IsFormedAndOnlineAndAllowTrading()` 返回 `true` 时才继续执行交易逻辑。
2. **手数校验**
   - 每个行情更新都会根据 `Security.MinVolume`、`Security.MaxVolume` 与 `Security.VolumeStep` 校验参数 `OrderVolume`。
   - 手数必须落在允许范围内并且是步长的整数倍，否则记录日志并阻止提交订单，复刻 MT5 辅助函数的行为。
3. **保证金预检**
   - 在下单前估算多头和空头方向所需的保证金。计算使用最新的买卖价、合约乘数以及用户配置的 `AccountLeverage`。
   - 如果当前或初始账户权益不足以覆盖任一方向，则跳过当前行情推送，与 `CheckMoneyForTrade` 的保护逻辑一致。
4. **挂单管理**
   - 当没有活跃的买入止损单时，在当前卖价（四舍五入到最接近的最小报价单位）挂出新的买入止损单；卖出止损单则使用当前买价。
   - 每张挂单都带有一个本地维护的到期时间（`ExpirationMinutes`，默认一天）。后续行情会检查是否到期，到期后立即撤单并等待下一次行情重新补单。
5. **风险控制**
   - 通过 `StartProtection` 在成交后自动放置绝对距离的止损与止盈，距离由 `StopLossPoints` 和 `TakeProfitPoints` 决定，等价于 MT5 里下单时设置的 SL/TP 参数。

这样就形成了一套极简的突破策略：市场始终被两张止损单“夹住”。当其中一张被触发，另一侧继续留在市场中，下一次行情刷新时会再次补齐缺失的一侧。

## 参数

| 参数 | 说明 |
|------|------|
| `OrderVolume` | 每张止损单的手数，必须满足经纪商限制与手数步长。 |
| `StopLossPoints` | 成交后止损距离（点数），会转换为绝对价格。 |
| `TakeProfitPoints` | 成交后止盈距离（点数）。 |
| `ExpirationMinutes` | 每张挂单的有效时间。到期后自动撤单并在下一次行情重建。 |
| `AccountLeverage` | 估算保证金时使用的账户杠杆。 |

所有点数都会通过 `Security.PriceStep` 转换为真实的价格偏移。如果交易品种缺少价格步长或合约乘数，则使用 1 作为兜底值以确保公式可用。日志会提示任何异常配置，方便快速调整。

## 实现细节

- **订单生命周期**：策略保存 `BuyStop` 与 `SellStop` 返回的 `Order` 对象，并在订单进入 `Done`、`Canceled` 或 `Failed` 状态时清理引用，避免把历史订单当成活动订单。
- **有效期控制**：由于不同市场对止损单有效期的支持不一，这里采用本地计时方式，在超时后调用 `CancelOrder` 撤单。
- **保证金估算**：使用账户权益与配置的杠杆进行粗略估算，既贴近 `OrderCalcMargin` 的效果，又不依赖特定交易所接口。
- **高级 API 使用**：全部逻辑基于 `SubscribeLevel1`、`BuyStop`、`SellStop` 与 `StartProtection` 实现，符合转换指南并保持代码简洁。

本文档刻意提供尽可能详细的说明，便于读者理解转换过程并根据自身经纪商的要求调整参数。
