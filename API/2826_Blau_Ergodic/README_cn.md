# Blau Ergodic策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MQL5 平台 **Exp_BlauErgodic** 顾问在 StockSharp 上的移植版本。通过对动量及其绝对值进行三级 EMA 平滑，
构建 Blau Ergodic 振荡器的归一化主线与信号线，并提供三个与原版一致的信号模式。

默认订阅已完成的 4 小时K线。可以选择不同的价格来源（收盘价、开盘价、各种平均价），调整每一级平滑长度，
以及指定读取信号的柱索引 `SignalBar`。仓位规模由策略的 `Volume` 属性控制，可分别禁用多空开仓或平仓标志。
止损和止盈以点数设置，并通过 `Security.PriceStep` 转换成绝对价格。

## 信号模式

- **Breakdown**：关注振荡器穿越零轴。指标由负转正时开多，由正转负时开空；当振荡器保持在相反的区域时
  平掉现有仓位。
- **Twist**：寻找斜率反转。如果上一根柱子仍在下行而最新柱子转为上行则出现多头信号；空头信号为相反情况。
- **CloudTwist**：监控振荡器与信号线的交叉。穿越信号云向上时开多，跌回信号线下方时开空。

所有模式都以 `SignalBar` 指定的已完成柱（默认 `1`，即上一根完成的K线）为当前值，并结合更早的数值进行确认。
由于策略只处理收盘后的数据，请将 `SignalBar` 设为不小于 `1`。

## 进出场规则

- **做多**：`AllowBuyEntry = true` 且当前净头寸不为多头（`Position <= 0`）时，只要所选模式给出买入条件就会建仓。
  如果存在空头敞口，系统会一次性买入 `Volume + |Position|` 以反向并建立多单。
- **做空**：`AllowSellEntry = true` 且当前净头寸不为空头（`Position >= 0`）时，模式触发卖出条件就会建立空单，
  同时平掉可能存在的多头仓位。
- **平多**：当模式给出反向信号或触发 `StopLossPoints`/`TakeProfitPoints` 时执行。被动退出会检查
  `AllowBuyExit`，而由止损/止盈触发的强制退出会忽略该标志以确保保护单有效。
- **平空**：与平多逻辑相同，使用 `AllowSellExit` 及相应的止损/止盈距离。

## 参数

- `CandleType`：订阅的K线类型（默认 4 小时）。
- `Mode`：`Breakdown`、`Twist` 或 `CloudTwist` 三种模式之一。
- `MomentumLength`：原始动量差分的长度。
- `First/Second/ThirdSmoothingLength`：动量级联 EMA 的长度。
- `SignalSmoothingLength`：信号线 EMA 的长度。
- `SignalBar`：用于读取信号的已完成柱索引（至少为 `1`）。
- `AppliedPrice`：振荡器使用的价格来源（收盘价、开盘价、均价等）。
- `AllowBuyEntry`、`AllowSellEntry`、`AllowBuyExit`、`AllowSellExit`：分别控制多空的开平仓权限。
- `StopLossPoints`、`TakeProfitPoints`：以点数表示的止损/止盈距离（通过 `Security.PriceStep` 转换）。

该移植使用 StockSharp 的高级 API（`SubscribeCandles`、`Bind`），保持 MQL5 原策略行为，并遵循项目对制表符缩进
与英文注释的要求。
