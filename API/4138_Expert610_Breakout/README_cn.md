# Expert610 突破策略

## 概述
Expert610 Breakout 策略是 MetaTrader 4 专家顾问 `Expert610.mq4` 的 C# 版本。原始机器人会在检测到一根大实体K线
后，将买入止损和卖出止损订单同时挂在上一根K线的上方和下方。仓位规模按照可承受风险的自由资金百分比计算，
止损和止盈距离以点（pip）表示。本 StockSharp 实现使用高级 API 完整复刻了该流程，并通过参数公开所有关键
调节项。

## 交易逻辑
1. **数据收集**
   - 订阅可配置的蜡烛类型并缓存最近收盘的K线。
   - 订阅盘口深度以估算当前买卖价差。如果缺少深度数据，则假定价差为零，从而与原始 EA 在无实时点差环境下
     的行为保持一致。
2. **波动性筛选**
   - 上一根K线最高价减去当前收盘价以及当前收盘价减去上一根K线最低价均需大于 `ThresholdPips`（自动换算为价
     格单位）。
   - 当前K线开盘价必须低于上一根最高价才能允许多头布单，同时必须高于上一根最低价才能允许空头布单。当两者
     同时成立时，策略会对称地下达两个挂单。
3. **挂单逻辑**
   - 买入止损价格设为 `上一高点 + BreakoutOffset + 价差`，完全复制 MT4 中按卖价入场的写法。
   - 卖出止损价格设为 `上一低点 - BreakoutOffset`，与原脚本一致地忽略买价端的点差补偿。
   - 任意时刻仅允许存在一组挂单；如果仍有未完成订单，新信号会被跳过。
4. **风险管理**
   - 手数以自由资金（`Portfolio.CurrentValue - Portfolio.BlockedValue`）乘以 `RiskPercent / 100` 得到。随后按照
     `RoundingDigits` 四舍五入，并使用与 MT4 相同的公式转换为手数：`lot = risk / stopPips * 0.1`，该公式假定
     0.1 手每 pip 对应 1 个账户货币单位。
   - 计算出的手数会根据交易所的 `VolumeStep`、`MinVolume`、`MaxVolume` 以及策略级的 `MinimumVolume` 进行调整，
     确保最终订单合法。
   - 调用 `StartProtection` 为所有成交仓位挂接以价格表示的止损与止盈，从而立即应用 `StopLossPips` 和
     `TakeProfitPips` 设置。

## 参数
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `RoundingDigits` | 风险和手数计算时使用的小数位数。 | `2` | 必须大于或等于 0。 |
| `RiskPercent` | 每笔交易占用的自由资金百分比。 | `1` | 设为 `0` 时禁用动态仓位，退回 `MinimumVolume`。 |
| `MinimumVolume` | 掛单最小手数下限。 | `0.1` | 同时会参考品种的 `MinVolume` 与 `VolumeStep`。 |
| `ThresholdPips` | 当前收盘价与上一根极值之间的最小距离。 | `5` | 使用检测到的 pip 大小进行换算。 |
| `BreakoutOffsetPips` | 在上一高/低点基础上附加的缓冲。 | `2` | 多空两侧对称应用。 |
| `StopLossPips` | 成交后附带的止损距离。 | `5` | 以 pip 表示，并传递给 `StartProtection`。 |
| `TakeProfitPips` | 成交后附带的止盈距离。 | `10` | 以 pip 表示；设为 `0` 可关闭止盈。 |
| `CandleType` | 用于判定突破的蜡烛类型。 | 1 小时时间框架 | 支持任意 StockSharp `DataType`。 |

## 实现细节
- Pip 大小依据品种的 `PriceStep` 与 `Decimals` 推导（3 位和 5 位外汇品种会乘以 10），完全复现 MQL4 的换算逻辑。
- 手数调整遵循 `VolumeStep`，并在限制范围内夹紧到 `MinVolume`/`MaxVolume`，最后再与策略参数
  `MinimumVolume` 取最大值，确保订单合法。
- 点差补偿使用已订阅盘口中的最优买/卖价，在具备实时点差的环境中可与 MT4 版本匹配；若数据缺失则自动退化为
  零补偿。
- 当订单状态变为成交、取消或失败后，从内部状态中移除引用，以便下一根符合条件的K线可以重新挂单。

## 与 MQL 版本的差异
- 原 EA 使用 `Digits2Round` 对风险和手数同时取整。移植版本保留该逻辑，并额外按照交易所的手数步长做对齐。
- MT4 中在下单时直接附带止损/止盈价格；在 StockSharp 中改为通过 `StartProtection` 自动挂设保护单。
- 自由资金改为读取投资组合的 `CurrentValue` 与 `BlockedValue`；若数据缺失则退回固定手数模式。
- 所有计算均在已收盘的蜡烛上进行，避免盘中反复触发，并与原脚本在K线结束时的判断保持一致。
