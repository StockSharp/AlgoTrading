# Tengri 策略（StockSharp 版本）

本策略基于 MetaTrader 顾问 *Tengri*，并使用 StockSharp 高阶 API 重新实现。原版顾问通过 RSI、私有的 “Silence” 波动过滤器以及 EMA 趋势判定，在 EURUSD 与 USDCHF 上构建网格并逐步加仓。移植过程中保留了主要逻辑，同时适配了 StockSharp 的净持仓模型。

## 核心思想

- **方向判断**：比较当前买价与较高周期（默认 30 分钟）K 线开盘价，若买价更高则偏多，若更低则偏空。
- **动量过滤**：在 1 小时 K 线上计算 14 周期 RSI，做多要求 RSI 低于 70，做空要求 RSI 高于 30。
- **“Silence” 过滤**：原始自定义指标以 ATR + EMA 平滑替代，分别在两个周期上运行，只有在波动低于阈值时才允许入场或加仓。
- **趋势确认**：中等周期 EMA 必须支持当前方向——只有当价格在 EMA 之上才允许继续做多，在 EMA 之下才允许继续做空。
- **加仓与马丁量化**：首个订单使用固定手数或按净值比例计算。随后每次加仓都将上一次成交量乘以设定系数（默认在 `StepX` 前为 1.70，之后为 2.08）。
- **间距控制**：加仓间隔以两个基础点距（默认 10 与 20 点）为基础，可通过 `PipStepExponent` 指数放大。

## 交易流程

1. **入场检查**（按 `EntryCandleType` 周期，默认 M1）：
   - 根据 `DealCandleType` 蜡烛确定方向。
   - 检查 RSI 与第一个 “Silence” 过滤器。
   - 确保本方向尚无仓位。若存在反向仓位，先行平仓（StockSharp 为净持仓模型）。
   - 计算下单手数并以市价入场，同时记录第一个仓位的止盈目标。
2. **加仓检查**（按 `ScaleCandleType` 周期，默认 M1）：
   - 确认 EMA 趋势方向正确且第二个 “Silence” 过滤器低于阈值。
   - 根据点距规则确认价格已远离最近的成交价。
   - 若满足条件且未超过 `MaxTrades` 上限，则以马丁手数再次市价成交。
3. **持仓管理**：
   - 可选的全局止盈 (`UseLimit`) 在多空仓位同时存在且总浮盈超过 `Equity / LimitDivisor` 时平仓。
   - 第一个仓位设置固定止盈价位，价格触及后平掉全部净头寸。
   - 策略不设自动止损，与原始版本一致。

## 参数说明

| 参数 | 说明 |
|------|------|
| `DealCandleType` | 用于计算方向的高周期 K 线类型。 |
| `EntryCandleType` | 入场条件评估周期。 |
| `ScaleCandleType` | 加仓条件评估周期。 |
| `MaCandleType` | EMA 趋势使用的周期。 |
| `Silence1CandleType` / `Silence2CandleType` | 两个 ATR+EMA 波动过滤器的周期。 |
| `RsiPeriod` | RSI 周期，默认 14。 |
| `SilencePeriod1/2`、`SilenceInterpolation1/2`、`SilenceLevel1/2` | ATR 平滑参数与阈值。 |
| `MaPeriod` | EMA 周期。 |
| `PipStep`、`PipStep2`、`PipStepExponent` | 加仓点距控制。 |
| `LotExponent1`、`LotExponent2`、`StepX` | 马丁加仓手数倍数。 |
| `LotSize`、`FixLot`、`LotStep` | 首单资金管理设定。 |
| `SlTpPips` | 首单止盈点数（0 表示不设置）。 |
| `MaxTrades` | 单方向最多订单数。 |
| `UseLimit`、`LimitDivisor` | 全局止盈配置。 |
| `CloseFriday`、`CloseFridayHour` | 周五晚间停止开仓的过滤器。 |

## 与原始版本的差异

- **Silence 指标替换**：使用 ATR+EMA 近似原始的 Silence 指标，阈值保持相同数值，但可根据市场调整。
- **净持仓模式**：StockSharp 合并同一标的的多空持仓，因此策略在开仓前会先平掉反向仓位，而非同时持有多空。
- **止盈处理**：原版仅为首单设置止盈，移植版本在触发时平掉全部净仓，后续加仓保持无止盈以匹配原始风险结构。
- **交易标的**：策略交易当前 `Security`，需分别实例化以同时操作 EURUSD、USDCHF 等不同品种。

## 使用建议

- 请确认品种的 `VolumeStep`、最小/最大交易量，以便手数归一化满足交易所或券商规则。
- 策略依赖 Level1（bid/ask） 数据来判断方向及止盈，若行情源不提供需提前处理。
- 无止损意味着需要外部风险控制（如资金保护、人工监控等）。

## 图表与监控

建议在交易终端中订阅策略使用的各个 K 线序列，并叠加 EMA 及 ATR 曲线，方便验证过滤条件与网格距离是否符合预期。
