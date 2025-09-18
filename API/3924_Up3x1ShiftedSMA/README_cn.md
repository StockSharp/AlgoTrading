# Up3x1 移位SMA策略（MT4版本移植）

## 概览
- 将 `MQL/8097` 中的 MetaTrader 4 智能交易系统 `up3x1.mq4` 移植到 StockSharp 高层 API。
- 保留三个简单移动平均线（SMA）并向右偏移6根K线的原始计算方法。
- 仅在K线收盘后处理数据，模拟脚本里 `Volume[0] > 1` 的单次决策约束。
- 支持止盈、止损、亏损后自动减仓及可选的移动止损等风控功能。

## 交易逻辑
1. **指标**：三个带有图表偏移的SMA（默认周期分别为24、60、120）。
2. **做多条件**：
   - 前一根K线：`SMAfast₍t-1₎ < SMAmedium₍t-1₎ < SMAslow₍t-1₎`；
   - 当前K线：`SMAmedium₍t₎ < SMAfast₍t₎ < SMAslow₍t₎`，对应原代码的 `ma1 < ma2 < ma3 && ma5 < ma4 < ma6`。
3. **做空条件**：
   - 前一根K线：`SMAfast₍t-1₎ > SMAmedium₍t-1₎ > SMAslow₍t-1₎`；
   - 当前K线：`SMAmedium₍t₎ > SMAfast₍t₎ > SMAslow₍t₎`。
4. **离场规则**：
   - 止盈、止损按照设定的点数并结合 `Security.PriceStep` 计算价格距离。
   - 移动止损在浮盈超过 `TrailingStopPoints` 后跟踪最高/最低价。
   - 当均线次序反转时强制平仓，对应 MQL 中的 `OrderClose` 逻辑。

## 仓位管理
- 当无法获取组合权益时，默认下单量为 `BaseVolume`（0.1手）。
- 若可读取 `Portfolio.CurrentValue`，则按 `RiskFraction`（默认 `0.00002`，等价于 `FreeMargin * 0.02 / 1000`）计算动态仓位。
- 连续亏损次数大于1时，按照 `volume * losses / 3` 减少下单量，与 `LotsOptimized` 完全一致。
- 下单量根据 `Security.VolumeStep` 向下取整，若低于 `Security.MinVolume` 则放弃交易。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FastPeriod` | 24 | 最快移位SMA的周期。 |
| `MediumPeriod` | 60 | 中速移位SMA的周期。 |
| `SlowPeriod` | 120 | 最慢移位SMA的周期。 |
| `TakeProfitPoints` | 150 | 距离入场价的止盈点数。 |
| `StopLossPoints` | 100 | 距离入场价的止损点数。 |
| `TrailingStopPoints` | 100 | 移动止损点数（设为0可关闭）。 |
| `BaseVolume` | 0.1 | 默认下单量及减仓后的最小值。 |
| `RiskFraction` | 0.00002 | 组合权益乘数，用于动态仓位。 |
| `CandleType` | 1小时K线 | 指标所使用的K线类型。 |

## 转换说明
- 使用 `SubscribeCandles` 和 `Bind` 实现高层事件驱动，无需自建历史缓存。
- 通过保存上一根K线的指标数值模拟 MQL 的 `shift` 行为。
- 以市价单执行止盈/止损/移动止损，保持与 StockSharp 抽象模型兼容。
- 代码中的注释均为英文，符合项目要求。

## 使用建议
1. 在 StockSharp Designer 或代码中绑定策略到具体的证券与投资组合。
2. 根据实际需要调整 `CandleType`（默认H1）。
3. 根据品种最小价格变动调整各类点数参数（例如外汇常见的0.0001）。
4. 不需要移动止损时将 `TrailingStopPoints` 设为0。
5. 关注日志中的 “Enter long/short” 与 “Exit long/short” 提示以监控策略行为。

## 目录结构
```
API/3924/
├── CS/Up3x1ShiftedSmaStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```

## 免责声明
量化交易存在较大风险。该策略仅用于教学示例，实盘使用前必须充分回测与模拟验证。
