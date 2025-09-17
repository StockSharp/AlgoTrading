# N7S AO 772012 策略

## 概述

本策略是 MetaTrader 专家顾问 **N7S_AO_772012** 的 StockSharp 版本。原始机器人通过在多个时间框架上对 Awesome Oscillator (AO) 进行类似感知器的组合过滤，再配合价格模式门控以及可配置的 “neuro” 模式来决定最终的买卖方向。本转换版本完整保留了该决策树，并把所有调节参数暴露为策略参数。

策略运行在所选交易品种上，使用以下数据：

- **M1 K线**：用于入场时机及价格感知器。
- **H1 K线**：为多个 AO 感知器提供数据。
- **H4 K线**：计算基础 Buy/Sell 选择器 (BTS) 所用的 AO 动量差。

## 交易逻辑

1. 每当一根 M1 K线收盘时，策略会更新价格模式历史、处理已有仓位，并检查时间过滤（周一 02:00 之前及周五 18:00 之后不交易，使用平台本地时间）。
2. 小时级别的 AO 数值被送入五个感知器：
   - `Perceptron X/Y` – BTS 的基础多空过滤器，配合价格感知器和 H4 AO 差值使用。
   - `Neuro X/Y` – 在特定 neuro 模式下优先生效的高级多空过滤器。
   - `Neuro Z` – 在模式 4 中决定是否允许 Neuro X 发出信号的门控感知器。
3. 价格感知器对最新收盘价与若干过去开盘价的加权差值进行评估。
4. **NeuroMode** 参数决定大写感知器如何介入：
   - `4`：若 Neuro Z > 0，则仅当 Neuro X > 0 才能做多；否则只要 Neuro Y > 0 就做空；若均未触发则回退到 BTS。
   - `3`：Neuro Y > 0 时直接做空，否则回退到 BTS。
   - `2`：Neuro X > 0 时直接做多，否则回退到 BTS。
   - 其他值：跳过 neuro 层，直接执行 BTS。
5. **BTS** 模块结合价格感知器与 H4 AO 差：
   - 多头：价格感知器 > 0（或 `BtsMode = 0` 时忽略该条件）、BTS/Neuro X > 0、H4 AO 差 > 0。止损为 `BaseStopLossPointsLong`，止盈为 `BaseTakeProfitFactorLong × BaseStopLossPointsLong`。
   - 空头：价格感知器 < 0（或 `BtsMode = 0` 时忽略该条件）、BTS/Neuro Y > 0、H4 AO 差 < 0。止损为 `BaseStopLossPointsShort`，止盈为 `BaseTakeProfitFactorShort × BaseStopLossPointsShort`。
6. 信号成立后立即按市场价下单（需方向已启用）。策略内部追踪止损和止盈价格，每根收盘的 M1 K线都会根据最高价/最低价检查是否触发，并在触发时平仓。若出现反向信号，会先平掉已有仓位，等待下一根 K线再重新评估。

## 参数

### 交易
- **OrderVolume** – 市价单的基础手数。
- **AllowLongTrades / AllowShortTrades** – 启用或禁用多头/空头信号。
- **BtsMode** – 设为 `0` 时忽略价格感知器的符号过滤；否则价格感知器的符号必须与交易方向一致。
- **NeuroMode** – 控制高级感知器如何参与（见交易逻辑）。

### 基础 BTS 感知器
- **BaseStopLossPointsLong / BaseTakeProfitFactorLong** – 多头止损（点数）及其止盈倍数。
- **BaseStopLossPointsShort / BaseTakeProfitFactorShort** – 空头对应设置。
- **PerceptronPeriodX / Y** – 感知器使用的 AO 偏移量（单位：H1 K线数量）。
- **PerceptronWeightX1..4 / Y1..4** – 感知器权重 (0–100)，内部会减去 50 进行中心化。
- **PerceptronThresholdX / Y** – 感知器输出的绝对值需要达到的阈值。

### 价格过滤
- **PricePatternPeriod** – 价格感知器每个滞后项包含的 M1 K线数量。
- **PriceWeight1..4** – 价格感知器的权重（同样以 50 为中心）。

### Neuro 感知器
- **NeuroStopLossPointsLong / NeuroTakeProfitFactorLong** – Neuro X 信号的止损及止盈倍数。
- **NeuroStopLossPointsShort / NeuroTakeProfitFactorShort** – Neuro Y 信号的止损及止盈倍数。
- **NeuroPeriodX / Y / Z** – 三个 neuro 感知器使用的 AO 偏移量（H1 K线）。
- **NeuroWeightX1..4 / NeuroWeightY1..4 / NeuroWeightZ1..4** – 各感知器的权重。
- **NeuroThresholdX / NeuroThresholdY / NeuroThresholdZ** – 各感知器输出的绝对值阈值。

### 数据
- **CandleType** – 主交易时间框架（默认 1 分钟）。

## 交易管理

- 止损和止盈点数会根据品种的最小报价步长转换为绝对价格，若设置为 0 则表示不使用该防护。
- 每根 M1 K线收盘时对照最高价/最低价判断是否触发保护价位。
- 策略采用净持仓模式，不会同时持有多空仓。出现反向信号时先平仓再等待下一根 K线重新判定。

## 转换说明

- 使用 StockSharp 的高阶绑定接口 (`SubscribeCandles().Bind(...)`) 获取 AO 值，无需手动查询指标。
- 通过固定长度列表模拟原策略的历史偏移访问，同时避免直接调用指标准值。
- 根据需求未提供 Python 版本。
- 未对测试项目做出改动。
