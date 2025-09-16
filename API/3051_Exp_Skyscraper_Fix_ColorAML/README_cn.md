# Exp Skyscraper Fix ColorAML 策略

## 概述
该策略将 MetaTrader 5 专家顾问 **Exp_Skyscraper_Fix_ColorAML** 移植到 StockSharp 框架。系统包含两个独立的信号模块：

1. **Skyscraper Fix** – 基于 ATR 的通道，按照自适应边界的方向标记多头或空头趋势。
2. **ColorAML** – 自适应市场水平指标，通过比较局部分形区间来识别扩张或收缩阶段。

原始 MQL 版本为两个不同的 magic 值单独管理仓位，并允许同时持有多空。StockSharp 策略维护净头寸，因此相反信号会互相抵消，最终方向由最后一次进场决定。本说明书特别强调这些差异，便于在回测或交易时评估影响。

## 参数
### Skyscraper Fix 模块
- **SkyscraperCandleType** – 构建 Skyscraper Fix 指标的K线周期，默认为 `4h`。
- **SkyscraperEnableLongEntry / SkyscraperEnableShortEntry** – 是否允许开多或开空。
- **SkyscraperEnableLongExit / SkyscraperEnableShortExit** – 是否允许平掉现有的多头或空头。
- **SkyscraperLength** – 计算阶梯步长所用的 ATR 样本数量，默认 `10` 根。
- **SkyscraperMultiplier** – 乘以 ATR 的系数，默认 `0.9`。
- **SkyscraperPercentage** – 中线的额外百分比偏移，设为 0 表示关闭。
- **SkyscraperMode** – 选择按 High/Low 还是按 Close 构建通道。
- **SkyscraperSignalBar** – 读取颜色缓冲区时回溯的已完成K线数，至少为 `1`。
- **SkyscraperVolume** – 每次入场使用的下单手数。
- **SkyscraperStopLoss / SkyscraperTakeProfit** – 以价格步长表示的止损/止盈距离。

### ColorAML 模块
- **ColorAmlCandleType** – ColorAML 使用的K线周期，默认 `4h`。
- **ColorAmlEnableLongEntry / ColorAmlEnableShortEntry** – 是否允许开多或开空。
- **ColorAmlEnableLongExit / ColorAmlEnableShortExit** – 是否允许平掉现有仓位。
- **ColorAmlFractal** – 分形区间长度，默认 `6`。
- **ColorAmlLag** – 自适应平滑的滞后参数，默认 `7`。
- **ColorAmlSignalBar** – 读取颜色缓冲区时回溯的已完成K线数。
- **ColorAmlVolume** – ColorAML 模块开仓时使用的手数。
- **ColorAmlStopLoss / ColorAmlTakeProfit** – 止损与止盈距离，以价格步长表示。

## 交易逻辑
策略订阅两个模块各自的K线，只处理已经收盘的蜡烛。两个指标均按照原始 MQL 公式用 C# 重写：

- 当 **Skyscraper Fix** 的颜色变为 **0**（看涨），模块会在允许的情况下平掉空头，并在颜色变化时准备开多；颜色变为 **1**（看跌）时，平掉多头并准备开空。
- **ColorAML** 通过比较分形区间判断趋势。颜色 `2` 表示上涨扩张，关闭空头并可选择开多；颜色 `0` 表示下跌扩张，关闭多头并可选择开空；颜色 `1` 表示保持现状。

每次进场的下单量为 `配置手数 + |当前净头寸|`，因此在没有对冲功能的情况下也可以一次性反手并完成仓位切换。

## 风险管理
策略启动时会调用 `StartProtection()`。当某个模块开仓后，会记录入场价，并根据该模块的参数计算止损和止盈价位。之后若蜡烛的最高价或最低价触及阈值，就会触发市价单离场。将距离设为 0 可以关闭对应的保护。

## 实现说明
- Skyscraper Fix 与 ColorAML 的计算逻辑直接移植，全部在内部缓冲区完成，无需手动添加额外指标。
- StockSharp 使用净头寸模式，原策略中的多空同时持仓在此会相互抵消，需提前了解。
- 仅处理已完成的K线；`SignalBar` 需大于等于 `1`，未实现逐笔级别的判断。
- 止损和止盈通过检测蜡烛极值实现，而非服务器端委托，这与转换后框架的能力一致。

## 使用方法
1. 将策略绑定到目标证券与投资组合。
2. 根据需要配置两个模块的参数，确保相应的K线数据可用。
3. 启动策略，系统会自动订阅K线、计算颜色并按信号发送市价单。
4. 通过日志或图表监控趋势切换、风险管理事件以及成交记录。
