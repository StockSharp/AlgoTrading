# 六指标动量策略

该策略使用 StockSharp 高级 API 复刻 MetaTrader 4 专家顾问 **6xIndics_M**。它将来自比尔·威廉姆斯 Accelerator Oscillator（AC）和 Awesome Oscillator（AO）的六个动量输入组合在一起，通过可配置的判定矩阵生成交易信号，并用慢速随机指标作为最终过滤器。策略始终只保持一笔仓位，并提供马丁格尔加仓、止损/止盈以及可选的追踪止损，以完全对应原始 EA 的行为。

## 策略流程

1. **数据订阅**：根据参数 `CandleType`（默认 1 小时 K 线）订阅主行情序列。
2. **指标计算**：
   - Awesome Oscillator 计算 5/34 周期中价简单移动平均的差值。
   - 对 AO 做 5 周期简单移动平均得到 Accelerator Oscillator（AC）。
   - 使用 5/5/5 参数的随机指标，读取上一根已完成 K 线的 %K 值（MT4 中的 shift=1）。
3. **六个指标槽位**：每根收盘 K 线写入以下缓冲区：
   - 槽位 0：AC 延迟 1 根的数值 `AC[1]`。
   - 槽位 1：AC 延迟 10 根的数值 `AC[10]`。
   - 槽位 2：AC 延迟 20 根的数值 `AC[20]`。
   - 槽位 3：AO 动量 `AO[0] - AO[shift]`，shift 由 `AoMomentumShift` 控制。
   - 槽位 4：AC 动量 `AC[0] - AC[shift #1]`，shift 由 `AcPrimaryShift` 控制。
   - 槽位 5：AC 动量 `AC[0] - AC[shift #2]`，shift 由 `AcSecondaryShift` 控制。
4. **可配置判定矩阵**：参数 `FirstSourceIndex` ~ `SixthSourceIndex` 选择每个逻辑条件使用哪个槽位，完全对应 MT4 版本中的 `k/u/t/e/r/o` 变量。相同的信号也用于 `CloseOnReverseSignal` 功能，在出现反向信号时提前平仓。
5. **入场条件**：
   - **做多**：满足 `A > 0`、`B > 0.0001 × Sensitivity`、`C > 0.0002 × Sensitivity`、`D < 0`、`E < 0.0001 × Sensitivity`、`F < 0.0002 × Sensitivity`，且上一根随机指标 %K < 15。
   - **做空**：满足 `A < 0`、`B < 0.0001 × Sensitivity`、`C < 0.0002 × Sensitivity`、`D > 0`、`E > 0.0001 × Sensitivity`、`F > 0.0002 × Sensitivity`，且上一根随机指标 %K > 85。
6. **仓位管理**：
   - 与原始 EA 一样，同一时间只会持有一笔仓位，持仓期间不会生成新的开仓信号。
   - 止损、止盈以 “MT4 点数” 输入，并按照交易品种的最小跳动值（`Point` 概念）换算成价格。
   - 追踪止损在价格向有利方向移动 `TrailingStopPips`（如启用 `RequireProfitForTrailing`，还需额外的 `LockProfitPips`）后才会启动，并且只会向盈利方向收紧。
   - `CloseOnReverseSignal` 在仓位已盈利时出现反向信号（多头 Bid 高于开仓价、空头 Ask 低于开仓价）会立即平仓。
7. **马丁格尔手数**：启用后，若上笔交易亏损或打平，下一次下单量会乘以 `(TakeProfitPips + StopLossPips) / TakeProfitPips`；一旦盈利则恢复到基础手数 `Volume`。

## 参数说明

| 参数 | 作用 | 默认值 |
|------|------|--------|
| `AllowBuy` / `AllowSell` | 是否允许开多/开空。 | `true` |
| `CloseOnReverseSignal` | 盈利状态下出现反向信号时提前平仓。 | `false` |
| `FirstSourceIndex` … `SixthSourceIndex` | 选择 6 个逻辑条件使用的槽位编号（0~5，超出范围会被截断）。 | `1,2,3,4,3,4` |
| `AoMomentumShift` | 计算槽位 3 时 AO 的对比位移。 | `10` |
| `AcPrimaryShift` / `AcSecondaryShift` | 计算槽位 4、5 时 AC 的对比位移。 | `10` / `10` |
| `SensitivityMultiplier` | 乘到 0.0001 与 0.0002 阈值上的敏感度系数。 | `1.0` |
| `TakeProfitPips` / `StopLossPips` | 止盈、止损距离（MT4 点数，会根据最小跳动值换算）。 | `300` / `300` |
| `UseTrailingStop` | 是否启用追踪止损。 | `false` |
| `TrailingStopPips` | 追踪止损距离（点数）。 | `300` |
| `RequireProfitForTrailing` | 启用后，只有在多获得 `LockProfitPips` 利润后才开始移动止损。 | `false` |
| `LockProfitPips` | 追踪止损启动前需要锁定的额外利润（点数）。 | `300` |
| `Volume` | 基础下单量。 | `0.1` |
| `UseMartingale` | 是否启用马丁格尔加仓。 | `false` |
| `CandleType` | 计算所用的 K 线类型。 | `TimeSpan.FromHours(1)` |

## 使用建议

- 逻辑只在每根 K 线收盘后运行一次，复现了原 EA 中 `prevtime` 保护的节奏。
- 仅缓存所需的有限历史（最多 256 根），无需调用 `GetValue()` 即可实现 MT4 的 shift 访问，符合项目要求。
- 示例代码通过 K 线最高/最低价模拟止损和追踪止损触发；实盘建议使用真实止损单以确保成交。
- 马丁格尔手数会按照交易所的 `VolumeStep`、`MinVolume`、`MaxVolume` 自动对齐，避免无效手数。
- 即使关闭了某一方向的入场（`AllowBuy` 或 `AllowSell`），反向信号仍可用于 `CloseOnReverseSignal` 的提前平仓。

## 与 MT4 版本的差异

- 使用 StockSharp 自带的 Awesome Oscillator 与 SMA 指标，无需手工处理指标缓冲区。
- 开仓通过 `BuyMarket`/`SellMarket` 市价单完成，平仓使用 `ClosePosition()`，而 MT4 版本直接调用 `OrderSend`/`OrderClose`。
- 手数会根据交易品种的最小/最大手数限制以及步长自动归一化。
- 额外绘制 `DrawCandles`、`DrawIndicator`、`DrawOwnTrades`，方便在有图表界面时进行可视化验证。
