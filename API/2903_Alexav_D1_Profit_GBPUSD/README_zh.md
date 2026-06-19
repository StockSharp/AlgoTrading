# Alexav D1 Profit GBPUSD 策略
[English](README.md) | [Русский](README_ru.md)

基于日线级别的 GBP/USD 突破策略，结合高价 EMA、RSI 滤波、MACD 动量确认以及 ATR 风险管理。此实现完整复刻了原始 MetaTrader 版本的四层分批止盈与保本移动规则。

## 核心信息

- **市场**：GBP/USD 现货或差价合约
- **时间框架**：日线（可配置）
- **方向**：多空双向
- **仓位结构**：每次信号分四笔入场，共用止损
- **技术指标**：高价 EMA、RSI、MACD 主线、ATR

## 指标设置

1. **高价 EMA** – 默认周期 6，用于确定突破基准。
2. **RSI** – 默认周期 10，设定动量区间过滤。
3. **MACD 主线** – 快线 5、慢线 21、信号线 14，仅使用主线判断动量加速度。
4. **ATR** – 周期 28，提供波动性自适应的止损与目标。

## 入场逻辑

### 多头

1. 前一根日线开盘价低于 EMA(High)，收盘价高于 EMA(High)，形成向上穿越。
2. RSI 位于 **60** 与 **80** 之间，过滤动量不足或过度延伸的行情。
3. 满足以下任一 MACD 条件：
   - 两根之前的 MACD 主线值为负数，显示动量刚转正；
   - 近两根 K 线的 MACD 绝对值相对变化大于 **MacdDiffBuy**（默认 0.5）。

条件满足时，先平掉所有空头，再以相同数量提交 4 笔市价买单（默认每笔 0.1 手）。

### 空头

1. 日线开盘价高于 EMA(High)，收盘价跌至 EMA(High) 下方。
2. RSI 介于 **25** 与 **39** 之间，对应多头区间的镜像设置。
3. 满足以下任一 MACD 条件：
   - 两根之前的 MACD 主线值为正；
   - 近两根 MACD 绝对值的相对变化超过 **MacdDiffSell**（默认 0.15）。

确认后，若存在多头先行平仓，再发送四笔等量市价卖单。

## 仓位管理

- **初始止损**：按 ATR 设置共享止损。多头为 `entry - ATR * StopLossMultiplier`（默认 1.6），空头为 `entry + ATR * StopLossMultiplier`。
- **分批止盈**：每笔仓位对应四个 ATR 目标，系数依次为 `1.0`、`1.5`、`2.0`、`2.5`，再乘以 `TakeProfitMultiplier`（默认 1）。价格触及目标时以市价平掉四分之一仓位。
- **移动保本**：每次部分止盈后，将剩余仓位的保护价上移（或下移）到最新止盈价，复刻原 EA 的止盈触发后移至保本的处理。
- **止损触发**：若 K 线最高/最低触及保护价，立即市价全平剩余仓位。

## 风险控制

- 策略不会加码超过四笔初始仓位，未清空前的新信号会被忽略。
- ATR 必须有效（>0），指标未形成时跳过信号。
- 运行过程中参数变动只影响后续下单，入场时的每笔数量会被记录用于后续止盈。

## 参数列表

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 单笔市价单量 | `0.1` |
| `EmaPeriod` | 高价 EMA 周期 | `6` |
| `RsiPeriod` | RSI 周期 | `10` |
| `AtrPeriod` | ATR 周期 | `28` |
| `StopLossMultiplier` | ATR 止损倍数 | `1.6` |
| `TakeProfitMultiplier` | ATR 止盈倍数基准 | `1.0` |
| `MacdFastPeriod` | MACD 快线周期 | `5` |
| `MacdSlowPeriod` | MACD 慢线周期 | `21` |
| `MacdSignalPeriod` | MACD 信号线周期 | `14` |
| `MacdDiffBuyThreshold` | 多头最小 MACD 加速度 | `0.5` |
| `MacdDiffSellThreshold` | 空头最小 MACD 加速度 | `0.15` |
| `RsiUpperLimit` | 多头允许的最大 RSI | `80` |
| `RsiUpperLevel` | 多头需要的最小 RSI | `60` |
| `RsiLowerLevel` | 空头允许的最大 RSI | `39` |
| `RsiLowerLimit` | 空头需要的最小 RSI | `25` |
| `CandleType` | 订阅的 K 线类型 | `1 Day` |

## 实践建议

- 联合优化 RSI 与 MACD 阈值，避免只放宽 RSI 而造成假信号。
- 分批止盈依赖蜡烛高低点，回测需保证数据质量。
- 需准备足够保证金以承载四笔同时在途的订单。
