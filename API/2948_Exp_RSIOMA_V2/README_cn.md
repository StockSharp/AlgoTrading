# Exp RSIOMA V2 策略

## 概述
Exp RSIOMA V2 策略基于 MetaTrader 5 平台的同名专家顾问，将其迁移到 StockSharp 高级 API。策略依旧围绕 RSIOMA（移动平均的 RSI）指标运行：先对价格进行平滑处理，再计算动量，随后构建 RSI 式的加权平均，以监测指标进入或离开关键区域时的交易机会。

## 交易逻辑
1. **价格预处理**：从 K 线中选取 `AppliedPrice` 指定的价格（默认收盘价），并使用四种可选移动平均之一（简单、指数、平滑或加权）进行平滑。
2. **动量计算**：将平滑后的价格与 `MomentumPeriod` 根 K 线之前的值相减，得到当前动量。
3. **RSIOMA 计算**：把正动量与负动量分别进行长度为 `RsiomaLength` 的指数平滑，构成 0 到 100 之间的 RSIOMA 数值。
4. **信号判定**：根据 `Mode` 设定检查最近的已完成 K 线：
   - **Breakdown**：当 RSIOMA 离开主要趋势区 (`MainTrendLong` / `MainTrendShort`) 时触发。指标从上方区间跌回通道时平掉空头并允许做多，反之亦然。
   - **Twist**：寻找拐点。当 RSIOMA 的斜率由下降转为上升时买入，由上升转为下降时卖出。
   - **CloudTwist**：模拟 MT5 指标中的彩云，当 RSIOMA 从超买 / 超卖区回到通道内时开仓，并同时关闭相反方向的持仓。

信号在 `SignalBar` 指定的已收 K 线上进行评估（默认上一根 K 线），确保只使用完全确认的数据。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 市价单使用的默认下单量。 | `1` |
| `CandleType` | 策略订阅的 K 线类型。 | `4 小时` |
| `EnableLongEntries` / `EnableShortEntries` | 是否允许开多 / 开空。 | `true` |
| `EnableLongExits` / `EnableShortExits` | 是否允许平多 / 平空。 | `true` |
| `Mode` | 交易模式（Breakdown、Twist 或 CloudTwist）。 | `Breakdown` |
| `PriceSmoothing` | 在计算 RSIOMA 前对价格使用的移动平均方式。 | `Exponential` |
| `RsiomaLength` | RSIOMA 平滑周期。 | `14` |
| `MomentumPeriod` | 动量计算时的滞后周期。 | `1` |
| `AppliedPrice` | 指标使用的价格（收盘、开盘、中值、DeMark 等）。 | `Close` |
| `MainTrendLong` / `MainTrendShort` | 定义超买 / 超卖区的 RSIOMA 阈值。 | `60` / `40` |
| `SignalBar` | 用于评估信号的已收 K 线数量。 | `1` |

## 实现说明
- 目前仅支持 StockSharp 中现成的四种平滑方式（SMA、EMA、SMMA、WMA），原始版本中的 JJMA、VIDYA、AMA 等高级算法未纳入实现。
- RSI 初值会使用前 `RsiomaLength` 个动量值进行初始化，从而与 MT5 EA 保持一致，之后采用指数递推更新。
- 在发出反向信号前会先平掉现有仓位。可通过 `EnableLongEntries` / `EnableShortEntries` 与 `EnableLongExits` / `EnableShortExits` 控制允许的方向。
- `SignalBar = 0` 可用于响应当前已完成的 K 线，更大的数值则对应等待更多根 K 线确认的逻辑。

## 使用步骤
1. 将策略添加到 StockSharp 项目中，并指定交易品种。
2. 通过 `CandleType` 设置 K 线周期（默认 4 小时），并根据品种波动性调整阈值参数。
3. 根据需求选择交易模式：突破型（Breakdown）、拐点型（Twist）或云颜色切换型（CloudTwist）。
4. 启动策略后，会自动订阅所选 K 线，计算 RSIOMA 指标，并在条件满足时发送市价单。
