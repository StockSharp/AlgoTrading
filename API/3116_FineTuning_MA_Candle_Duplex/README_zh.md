# FineTuning MA Candle Duplex 策略

## 概览
- 将 MetaTrader 5 顾问 **Exp_FineTuningMACandle_Duplex** 移植为 C# 版本。
- 复制 FineTuningMA 烛图指示器的两套独立流，可分别调整多头与空头逻辑。
- 基于 StockSharp 的高级 API：行情订阅、指标处理、风控与图形输出全部交由框架处理。

## FineTuningMA 烛图模型
- 原始指标对最近 `Length` 根 K 线应用三段指数权重（`Rank1`–`Rank3`）与偏移系数，构造合成开/高/低/收价。
- 合成的开盘价与收盘价比较后生成颜色编码：`2` 表示多头，`1` 表示中性，`0` 表示空头。
- 当真实 K 线实体小于阈值 `Gap` 时，合成开盘价会被锁定为上一根的合成收盘价，忠实还原原版的“平头”处理。
- 本移植版本的指标仅输出颜色序列（0/1/2），因为交易信号完全基于颜色变化。

## 交易逻辑
1. 同时订阅两个烛图流（`LongCandleType` 与 `ShortCandleType`），可以是相同或不同的时间框架。
2. 每个流使用一套独立的 FineTuningMA 指标参数以及信号偏移量 `SignalBar`。
3. 对于每根完结的烛图执行以下规则：
   - **平多**：前一颜色为 `0` 时关闭现有多头。
   - **做多**：前一颜色为 `2` 且当前颜色不再是 `2` 时买入（若持有空头会先反手）。
   - **平空**：前一颜色为 `2` 时回补空头。
   - **做空**：前一颜色为 `0` 且当前颜色不再是 `0` 时卖出（若持有多头会先反手）。
4. `OrderVolume` 控制基础下单量；当需要反手时会自动加上当前仓位的绝对值，使仓位一次性翻转。
5. 可选风控参数 `TakeProfitPoints` 与 `StopLossPoints` 会转换为价格点差并通过 `StartProtection` 激活。

## 参数
### 多头流
- `LongCandleType` —— 计算多头指标所用的烛图类型/时间框架。
- `LongLength` —— 加权窗口的柱数。
- `LongRank1`、`LongRank2`、`LongRank3` —— 控制权重分布的指数参数。
- `LongShift1`、`LongShift2`、`LongShift3` —— 0…1 范围内的偏移系数，用于向窗口前/后段倾斜权重。
- `LongGap` —— 真实实体不超过该值时，合成开盘价保持为上一合成收盘价。
- `LongSignalBar` —— 读取信号前需要回看的已完成柱数量（`0` 表示上一根收盘柱，`1` 表示再往前一根，依此类推）。
- `EnableLongEntries` —— 是否允许开多。
- `EnableLongExits` —— 是否允许自动平多。

### 空头流
- `ShortCandleType` —— 计算空头指标的烛图类型。
- `ShortLength`、`ShortRank1`、`ShortRank2`、`ShortRank3`、`ShortShift1`、`ShortShift2`、`ShortShift3`、`ShortGap`、`ShortSignalBar` —— 与多头参数含义一致，但作用于空头流。
- `EnableShortEntries` —— 是否允许开空。
- `EnableShortExits` —— 是否允许自动平空。

### 交易参数
- `OrderVolume` —— 基础下单数量，反手时会加上当前仓位绝对值。
- `TakeProfitPoints` —— 以价格点表示的止盈距离（0 表示关闭）。
- `StopLossPoints` —— 以价格点表示的止损距离（0 表示关闭）。

## 备注
- 原顾问包含按余额或保证金计算的资金管理模式，本移植版保留为固定下单量 `OrderVolume`，请自行调整适合的仓位规模。
- 只有在标的提供有效最小价位 (`Security.Step > 0`) 时才会启动 `StartProtection`。
- 按要求未提供 Python 版本。
- 若多空使用不同时间框架，会生成两个独立图表区域；相同时间框架仅显示一张图。
- 策略只处理已完结的 K 线，忽略所有盘中更新。
