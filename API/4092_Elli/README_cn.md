# Elli 策略

## 概述
Elli 策略将 MetaTrader 4 的 "Elli" 专家顾问迁移到 StockSharp 的高级 API。原版脚本使用 1 小时 Ichimoku 结构配合低周期 ADX 过滤，同时内置固定的止损止盈。移植版本保留所有趋势判定规则，以 `StartProtection` 替代手工修改订单，并把每个调节参数封装为可优化的 `StrategyParam<T>`，方便在 Designer 中测试不同组合。

## 交易逻辑
1. **Ichimoku 趋势结构**
   - 订阅 `CandleType` 指定的主时间框（默认 H1），按照原始周期 19/60/120 计算 Tenkan、Kijun 与 Senkou。
   - 多头信号要求 Tenkan > Kijun > Senkou Span A > Senkou Span B，且蜡烛收盘价位于 Kijun 之上；空头条件完全相反。
   - Tenkan 与 Kijun 的绝对差值必须超过 `TenkanKijunGapPips`（以点为单位），以过滤掉云层平坦的行情。
2. **方向动能确认**
   - 第二个数据流在 `AdxCandleType`（默认 M1）上运行 Average Directional Index。
   - 当上一根 +DI 低于 `ConvertLow`、当前 +DI 突破 `ConvertHigh` 时才允许做多；做空则针对 −DI 检查同样的阈值，复制 MQL 中 `iADX` 的加速判断。
3. **下单执行**
   - 所有条件满足时，策略以 `OrderVolume + |Position|` 的数量发送市价单，若存在反向仓位会先行对冲关闭。
   - 系统始终保持单向持仓，与原脚本的 `OrdersTotal() < 1` 检查一致。
4. **风险控制**
   - `StartProtection` 根据点值换算出绝对价格，为订单附加对称的止损与止盈。
   - 持仓管理交由保护单完成，行为与 MT4 版本完全一致。

## 指标与数据订阅
- 主时间框：`CandleType`（默认 1 小时）用于 Ichimoku 计算。
- ADX 时间框：`AdxCandleType`（默认 1 分钟）用于监控 +DI/−DI。
- 指标：`Ichimoku`（Tenkan、Kijun、Senkou Span B）与 `AverageDirectionalIndex`。
- 若界面存在图表区域，可绘制蜡烛、指标及成交轨迹。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | `1` | 市价单的基础手数。 |
| `TakeProfitPips` | `60` | 止盈距离（点）。 |
| `StopLossPips` | `30` | 止损距离（点）。 |
| `TenkanPeriod` | `19` | Tenkan-sen 周期。 |
| `KijunPeriod` | `60` | Kijun-sen 周期。 |
| `SenkouSpanBPeriod` | `120` | Senkou Span B 周期。 |
| `TenkanKijunGapPips` | `20` | Tenkan 与 Kijun 的最小点差。 |
| `ConvertHigh` | `13` | 当前 DI 必须突破的阈值。 |
| `ConvertLow` | `6` | 前一根 DI 需要维持在其下方的阈值。 |
| `AdxPeriod` | `10` | ADX 计算周期。 |
| `CandleType` | `H1` | Ichimoku 使用的时间框。 |
| `AdxCandleType` | `M1` | ADX 使用的时间框。 |

所有参数均通过 `StrategyParam<T>` 暴露，可在 Designer 中优化与回测。

## 实现细节
- 点值转换遵循常见外汇习惯：五位报价使用 0.0001，三位报价使用 0.01，确保阈值与原策略一致。
- `_latestPlusDi`、`_previousPlusDi`、`_latestMinusDi`、`_previousMinusDi` 缓存 DI 序列，精确复现 `iADX(symbol, timeframe, period, mode, shift)` 中 shift=0/1 的比较。
- `IsFormedAndOnlineAndAllowTrading()` 确保所有数据与指标准备完毕后才允许下单，避免热身阶段的误触发。
- 下单量使用 `Volume + Math.Abs(Position)`，即刻扭转旧仓，保持单一方向的暴露。
