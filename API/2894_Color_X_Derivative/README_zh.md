# Color X Derivative 策略

## 概述
本策略是 MetaTrader 专家顾问“Exp_ColorXDerivative”的 StockSharp 版本。策略在可配置的 K 线周期（默认 12 小时）上运行，分析 ColorXDerivative 动量柱状图。指标会衡量所选价格在固定位移内的变化速度，用移动平均进行平滑，并把每个柱子划分为五种颜色状态。交易逻辑完全复刻原始 EA：当多头动能加速或空头走势开始收缩时做多，当空头压力增强或多头走势减弱时做空。

## 指标逻辑
1. 将每根 K 线转换为所选 `AppliedPrice`（收盘价、开盘价、加权收盘价、Demark 价格等）。
2. 计算价格导数：`(price[0] - price[shift]) * 100 / shift`，其中 `shift = DerivativePeriod`。
3. 使用所选平滑方法（`SMA`、`EMA`、`SMMA`、`LWMA` 或 `Jurik`）平滑导数。默认的 Jurik 平滑对应 MQL 库中的 JJMA。
4. 根据导数的符号和斜率分配颜色状态：
   - **0** – 导数大于 0 且继续上升（多头加速）。
   - **1** – 导数大于 0 但下降（多头动能减弱）。
   - **2** – 导数接近 0（中性）。
   - **3** – 导数小于 0 但上升（空头走势收缩）。
   - **4** – 导数小于 0 且下降（空头加速）。

`SignalShift` 控制读取哪根已完成的柱子（1 = 最近一根已收盘柱，2 = 前一根，以此类推）。

## 交易规则
- **做多入场**（`EnableLongEntry = true`）：
  - 当前颜色为 0 且前一颜色不为 0（多头动能突然增强）；或
  - 当前颜色为 3 且前一颜色为 4 或 2（空头走势开始收缩）。
- **做空入场**（`EnableShortEntry = true`）：
  - 当前颜色为 4 且前一颜色不为 4（空头动能增强）；或
  - 当前颜色为 1 且前一颜色为 0 或 2（多头动能减弱）。
- **平多**：当前颜色为 1 或 4 且 `EnableLongExit = true`。
- **平空**：当前颜色为 0 或 3 且 `EnableShortExit = true`。

策略始终使用 `OrderVolume` 的市场单成交，并在尝试开新仓前先执行平仓，以保持与原 EA 相同的顺序行为。

## 风险控制
`StopLossTicks` 与 `TakeProfitTicks` 提供可选的止损和止盈距离（按最小价位计算）。当任意数值大于零时会调用 `StartProtection`，把 tick 数转换为 `Security.Step` 所定义的价格步长，随后一次性启动止损/止盈保护，适用于实盘与回测。

## 参数说明
- `OrderVolume` – 市价单数量。
- `CandleType` – 计算指标的 K 线类型（默认 12 小时）。
- `DerivativePeriod` – 计算导数时使用的位移长度。
- `AppliedPrice` – 导数所使用的价格来源（收盘价、加权价、Demark 价等）。
- `SmoothingMethod` – 导数平滑方法（SMA、EMA、SMMA、LWMA、Jurik）。
- `SmoothingLength` – 平滑滤波器周期。
- `SignalShift` – 读取颜色值时回溯的已收盘柱数量（1 = 最近一根）。
- `StopLossTicks` / `TakeProfitTicks` – 以最小价位表示的止损与止盈距离，可选。
- `EnableLongEntry`、`EnableShortEntry`、`EnableLongExit`、`EnableShortExit` – 是否允许对应方向的入场/出场。

## 说明
- 策略专注于复制 MetaTrader 指标逻辑，没有加入额外的资金管理模块。
- Jurik 平滑是对 MQL SmoothAlgorithms 库中 JJMA 的最佳近似，其他枚举值映射到 StockSharp 自带的移动平均。
- 指标内部保存完整的颜色历史，因此在优化 `SignalShift` 时与原平台保持一致。
