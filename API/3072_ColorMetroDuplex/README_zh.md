# ColorMetroDuplexStrategy

## 概述

`ColorMetroDuplexStrategy` 是 MT5 专家顾问 **Exp_ColorMETRO_Duplex** 的 C# 版本。原始 EA 通过两个独立的 ColorMETRO 指标模块进行多空操作：每个模块订阅自己的 K 线周期，读取指标输出的快慢 RSI 台阶包络线，并在交叉时决定是否开仓或平仓。

移植后的 StockSharp 策略保留了这两个模块，并使用高级 API 处理行情订阅、订单执行和指标绑定。同时实现了自定义的 `ColorMetroIndicator`，完全复刻 MT5 中的 iCustom 指标逻辑，输出快线、慢线以及内部使用的 RSI 值。

## 工作流程

1. 启动时会创建两个 `SignalModule`：**Long**（多头模块）和 **Short**（空头模块），它们具有独立的周期与参数。
2. 每个模块调用 `SubscribeCandles` 订阅相应的周期，并使用 `BindEx` 与 `ColorMetroIndicator` 绑定。
3. 每根收盘的 K 线都会触发一次计算，指标返回：
   - 快速 ColorMETRO 包络线（依据快步长的 RSI 台阶）。
   - 慢速 ColorMETRO 包络线。
   - 指标内部的 RSI 数值（用于参考）。
4. 模块将最新结果加入历史缓存，并按照 `SignalBar` 指定的偏移量对比最近两根信号柱，复制 MT5 中 `CopyBuffer` 的用法。
5. 交易规则：
   - **多头模块**：
     - *开仓*：前一根信号柱快线在慢线上方，本柱快线下穿或等于慢线。
     - *平仓*：前一根信号柱慢线在快线上方。
   - **空头模块**：
     - *开仓*：前一根信号柱快线在慢线下方，本柱快线上穿或等于慢线。
     - *平仓*：前一根信号柱慢线在快线下方。
6. 下单使用 `BuyMarket` / `SellMarket`。策略始终检查净头寸，如果需要反向操作，会先平掉已有仓位再建立新仓。

## 参数说明

每个模块都拥有独立参数组，默认值与 MT5 EA 保持一致。

### 市场参数

- **Long_Volume**、**Short_Volume**：新开仓的手数。
- **Long_OpenAllowed**、**Short_OpenAllowed**：是否允许该模块开仓。
- **Long_CloseAllowed**、**Short_CloseAllowed**：是否允许信号自动平仓。
- **Long_MarginMode**、**Short_MarginMode**：保留的资金管理模式（本移植中不参与计算）。
- **Long_StopLoss**、**Long_TakeProfit**、**Long_Deviation** 以及空头对应参数：仅作为文档保留，本版本不会自动设置止损止盈。
- **Long_Magic**、**Short_Magic**：原 EA 的魔术号，便于对照。

### 指标参数

- **Long_CandleType**、**Short_CandleType**：各模块的 K 线周期。
- **Long_PeriodRSI**、**Short_PeriodRSI**：ColorMETRO 内部使用的 RSI 周期。
- **Long_StepSizeFast**、**Short_StepSizeFast**：快包络的步长（RSI 点数）。
- **Long_StepSizeSlow**、**Short_StepSizeSlow**：慢包络的步长。
- **Long_SignalBar**、**Short_SignalBar**：读取指标缓存时的偏移，与 MT5 的 `SignalBar` 相同。
- **Long_AppliedPrice**、**Short_AppliedPrice**：计算 RSI 时使用的价格，默认收盘价。

## 与 MT5 版本的差异

- **头寸模型**：MT5 可以通过不同魔术号同时持有多空仓位，StockSharp 采用净头寸模式，反手时会先平仓再开仓。
- **资金管理**：保留了 MarginMode、Deviation 等输入，但默认不参与下单计算，仓位大小由 Volume 参数控制。
- **止损/止盈**：原 EA 每次下单都会附带止损止盈。本策略仅记录相应距离，如需自动风控需自行扩展。
- **时间锁**：MT5 使用全局变量避免同一时间重复开仓；在 StockSharp 中我们按收盘柱执行一次逻辑，并通过净头寸判断避免重复信号。

## 备注

- `ColorMetroIndicator` 完全仿真原始算法，包含趋势记忆逻辑，可用于绘图或调试。
- 代码中提供了详细英文注释，便于进一步修改和优化。
- 若要加入自动止损、止盈或其他风险控制，可在 `ProcessModule` 中增加对应的订单逻辑。
