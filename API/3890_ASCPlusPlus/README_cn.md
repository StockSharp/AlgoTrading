[English](README.md) | [Русский](README_ru.md)

**ASC++ Williams Breakout 策略** 将经典的 MQL4 "ASC++.mq4" 专家顾问迁移到 StockSharp 的高级 API。策略在 Williams %R 振荡指标确认的窄幅波动区间内埋单，在蜡烛高点或低点外侧放置突破性止损单；订单被触发后，系统会自动设置止盈、止损以及（可选的）移动止损。

## 策略流程

1. **指标初始化**
   - 使用快、慢两个 Williams %R（默认周期 9 和 54）衡量短期动能。
   - 采用 10 周期 ATR 代替原始代码中手工实现的加权范围计算。
   - 根据 `RiskLevel` 动态计算阈值 `x1 = 67 + RiskLevel` 与 `x2 = 33 - RiskLevel`，重建自适应超买/超卖分界。
2. **信号累积**
   - 每根收盘蜡烛计算 `value2 = 100 - |%R_fast|`：当值低于 `x2` 表示向上突破的潜力，高于 `x1` 则提示向下突破。
   - 连续落在同一极值区间的蜡烛会累积计数，只有当连续次数达到 `SignalConfirmation`（默认 5）时才允许挂单，模拟原版的 `SigVal` 计时器。
3. **下单逻辑**
   - 只有在平均波动小于 `EntryRange` 且快慢 %R 方向一致时才会挂出突破单：
     - 多头：`High + ATR * 0.5 + EntryStopLevel * PriceStep` 的 Buy Stop。
     - 空头：`Low - ATR * 0.5 - EntryStopLevel * PriceStep` 的 Sell Stop。
   - 新信号出现时会自动取消相反方向的挂单，避免持仓冲突。
4. **仓位管理**
   - 通过 `StartProtection` 自动生成止盈、止损，并在 `TrailingStopPoints > 0` 时启用移动止损。
   - 如果当前持仓与新信号方向相反，策略会先平仓再重新挂出突破单，与原 EA 的处理方式一致。

## 参数说明

| 参数 | 默认值 | 作用 |
|------|--------|------|
| `CandleType` | 15 分钟 | 计算所用的基础蜡烛类型。 |
| `FastLength` | 9 | 快速 Williams %R 的周期。 |
| `SlowLength` | 54 | 慢速 Williams %R 的周期。 |
| `RangeLength` | 10 | ATR 平滑窗口，替代手工范围累加。 |
| `EntryStopLevel` | 10 点 | 在突破价位上额外增加的点数，用于避开噪音。 |
| `EntryRange` | 27 点 | 允许挂单的最大平均波动范围。 |
| `RiskLevel` | 3 | 调整 `x1`/`x2` 阈值，控制信号敏感度。 |
| `SignalConfirmation` | 5 根蜡烛 | 触发挂单所需的连续极值蜡烛数量。 |
| `TakeProfitPoints` | 100 点 | 自动止盈距离。 |
| `StopLossPoints` | 40 点 | 自动止损距离。 |
| `TrailingStopPoints` | 20 点 | 大于零时启用移动止损。 |

## 迁移细节

- 原始 EA 手动累加带权范围，本版本直接使用 `AverageTrueRange`（10 周期）来获得等效的平滑效果，并遵循项目禁止自建缓冲区的要求。
- MQL 中依赖分钟时间戳的 `SigVal` 计数器被替换为 `SignalConfirmation` 连续蜡烛统计，在不访问原始时间的前提下保持节奏。
- 订单通过 `BuyStop`/`SellStop` 辅助函数注册，并在下反向单之前取消旧的挂单，等价于 `OrderSend`/`OrderDelete`。
- 风险控制委托给 `StartProtection`，自动处理止盈、止损与移动止损，简化了原代码的多级 `TSLevel1/TSLevel2` 逻辑。
- 所有指标都通过订阅和 `Bind` 获取数值，不使用 `GetValue` 等低级访问方式，完全符合转换规范。

## 使用建议

- 确认交易品种已经设置 `PriceStep`（价格步长）；不同品种需要重新调整 `EntryStopLevel`、`EntryRange` 与风险参数。
- 在较低周期需要更高频交易时，可降低 `SignalConfirmation`；若想筛选更强的整理区，则提高该值。
- 建议在宿主程序中开启图表绘制，观察挂单位置是否与近期高低点吻合。
- 策略对点差与滑点十分敏感，投入实盘前务必在历史数据上进行回测与前向测试。
