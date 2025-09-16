# 回调系统策略

该策略将 MetaTrader 5 专家顾问 **“Rollback system”** 完整移植到 StockSharp 平台。它保留了原始脚本的核心理念：
仅在新交易日开始时（午夜）评估最近 24 根小时 K 线，以寻找走出极端行情后可能出现的反向回调。

## 交易逻辑

1. 策略基于小时级别的 `CandleType`（默认 1 小时）。
2. 只有在新的一天开始后的前三分钟内（00:00–00:03）才会计算信号，并且与 MQL 版本一样，周一和周五不会开仓。
3. 开仓前会确认当前没有任何持仓。
4. 每天会从最近 24 根已结束的 K 线中计算以下关键变量：
   - `Open_24_minus_Close_1`：24 小时前的开盘价与最新收盘价之间的差值。
   - `Close_1_minus_Open_24`：反向差值，反映上一交易日的净变化。
   - `Close_1_minus_Lowest`：最新收盘价距离当日最低价的距离。
   - `Highest_minus_Close_1`：最新收盘价距离当日最高价的距离。
5. 入场规则（所有阈值都会从点值转换为价格单位）：
   - **多头 #1**：前一天大幅下跌（`Open_24_minus_Close_1` 高于 `ChannelOpenClosePips`），且收盘价仍处于极低位置（`Close_1_minus_Lowest` 小于 `RollbackPips - ChannelRollbackPips`）。
   - **多头 #2**：前一天大幅上涨（`Close_1_minus_Open_24` 高于通道阈值），但收盘价显著低于当日最高点（`Highest_minus_Close_1` 大于 `RollbackPips + ChannelRollbackPips`）。
   - **空头 #1**：前一天大幅上涨且收盘价逼近最高点（`Highest_minus_Close_1` 小于 `RollbackPips - ChannelRollbackPips`）。
   - **空头 #2**：前一天大幅下跌但收盘价强势回升（`Close_1_minus_Lowest` 大于 `RollbackPips + ChannelRollbackPips`）。
6. 策略使用 `BuyMarket`/`SellMarket` 以设定的 `TradeVolume` 市价开仓。`StopLossPips` 和 `TakeProfitPips` 用于生成止损和止盈距离（参数为 0 时禁用）。
7. 每根完结的 K 线都会检查保护价位；如果 intrabar 价格触发止损或止盈，就会通过市价单平仓，从而模拟原始 MQL 脚本的硬性保护委托。

## 点值转换

MetaTrader 5 在 3 位和 5 位小数报价中会将点值扩大 10 倍。移植版本保留了这项逻辑：
策略读取品种的 `PriceStep`，并在检测到小数位数为 3 或 5 时乘以 10，从而确保阈值、止损与止盈距离与原有脚本保持一致。

## 参数

| 参数 | 说明 |
|------|------|
| `TradeVolume` | 市价开仓所使用的交易量。|
| `StopLossPips` | 止损距离（点）。设为 0 表示不启用。|
| `TakeProfitPips` | 止盈距离（点）。设为 0 表示不启用。|
| `RollbackPips` | 各个信号共享的基础回调要求。|
| `ChannelOpenClosePips` | 需要满足的开盘价与收盘价差值阈值。|
| `ChannelRollbackPips` | 回调判定时使用的容差。|
| `CandleType` | 工作级别，默认小时 K 线。|

## 注意事项

- 原脚本会在图表上绘制矩形标记，移植版本只保留交易逻辑。
- 风险控制通过策略内部监控来实现，而不是在交易所挂出实际的保护委托，这是为了与 StockSharp 高阶 API 的头寸管理方式保持一致。
- 进行参数优化时，请结合目标品种的点值和经纪商的最小报价单位调整阈值与下单量。
