# The 20s Breakout 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
本策略是 MetaTrader 专家顾问 **Exp_The_20s_v020** 的 C# 版本。它复现了“The 20s”指标寻找波动率压缩后突破的思路。算法只处理所选周期内已经收盘的 K 线，当价格穿越上一根 K 线 20% 区间时触发交易信号。实现完全使用 StockSharp 的高级 API，并允许分别开启或关闭多头/空头操作。

## 信号逻辑
指标按以下步骤分析最新的 K 线数据：

1. 计算上一根 K 线的波动区间：`range = high[1] - low[1]`。
2. 根据区间构建两个阈值：
   - `top = high[1] - range * Ratio`
   - `bottom = low[1] + range * Ratio`
3. 将当前 K 线与上述阈值及 `LevelPoints`（通过品种 `PriceStep` 转换为价格）进行比较。

原始脚本提供两种计算模式：

- **Mode1（默认）**：寻找上一根 K 线在 20% 区间内的假突破以及当前 K 线的强力反转。若 `IsDirect = true`，信号代表买入；若为 `false`，则反向使用信号。
- **Mode2**：要求信号前出现三根逐渐扩张的 K 线。当价格向下爆发并开在下方阈值之下时触发一个方向，若开在上方阈值之上则触发另一个方向。`IsDirect` 同样决定是否反向使用信号。

`SignalBar` 参数可以将执行延后若干根 K 线（0 表示当前 K 线，1 表示上一根，以此类推），完全对应原 EA 中对历史信号的处理方式。

## 交易管理
- **入场**：`AllowLongEntry` 和 `AllowShortEntry` 控制是否允许开多或开空；`OrderVolume` 决定每次新仓的数量。
- **反向**：当出现做多信号时，策略先根据 `AllowShortExit` 平掉当前空头仓位，再视情况开多；做空信号则对多头执行同样的流程。
- **止损/止盈**：`StopLossPoints` 与 `TakeProfitPoints` 以点数表示，通过 `PriceStep` 换算成价格，并在每根已完成 K 线上进行检查，一旦触及立即平仓。
- **方向切换**：`IsDirect = true` 表示沿用原始指标的方向，`false` 则对买卖信号进行翻转，方便在不同市场环境下使用。

## 参数
- `OrderVolume` – 默认 `1`。新开仓的手数。
- `StopLossPoints` – 默认 `1000`。止损距离（点数，`0` 表示关闭）。
- `TakeProfitPoints` – 默认 `2000`。止盈距离（点数，`0` 表示关闭）。
- `AllowLongEntry` / `AllowShortEntry` – 是否允许开多/开空。
- `AllowLongExit` / `AllowShortExit` – 是否允许在反向信号出现时平掉多头/空头仓位。
- `SignalBar` – 默认 `1`。执行信号前需要等待的 K 线数量。
- `LevelPoints` – 默认 `100`。用于确认突破的附加距离。
- `Ratio` – 默认 `0.2`。上一根 K 线区间的 20% 带宽。
- `IsDirect` – 默认 `false`。`true` 时使用原始方向，`false` 时翻转方向。
- `Mode` – 默认 `Mode1`。选择两种指标算法之一。
- `CandleType` – 默认 H1 周期。决定订阅的 K 线类型。

## 说明
- 策略只在 K 线收盘后计算，避免因为未完成的波动导致误判。
- 代码中的日志与注释均使用英文，以保持与 StockSharp 示例一致。
- 止损与止盈由策略内部管理，不需要额外的挂单，在仿真或实盘环境中表现一致。
- 可应用于任意品种，只需保证 `PriceStep` 正确以便点数转换为价格。
- 若希望更谨慎，可在更高周期上结合 `Mode2` 与较大的 `SignalBar`，模拟原 EA 的“确认后入场”逻辑。
