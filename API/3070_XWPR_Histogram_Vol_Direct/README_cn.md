# Exp XWPR Histogram Vol Direct 策略

## 概述

该策略是 MetaTrader 专家顾问 **Exp_XWPR_Histogram_Vol_Direct** 的 StockSharp 版本。策略保留了通过成交量加权
Williams %R、使用均线平滑并在柱状图颜色翻转时开仓的原始思想。所有交易均在完成的 K 线后执行，可选的
止损和止盈以价格跳动数表示。

## 核心流程

1. 在所选周期上计算 Williams %R。
2. 将指标值整体上移 50，与指定的成交量来源（勾子数或真实成交量）相乘，并用可配置的移动平均进行平滑。
3. 使用相同的移动平均对原始成交量进行平滑，用于重建指标带（HighLevel2/1、LowLevel1/2）。
4. 追踪柱状图斜率的颜色：上升时记为 `0`，下降时记为 `1`。策略根据 `SignalShift` 参数保存最近的颜色历史。
5. 当颜色发生变化时执行操作：
   - `0 → 1`：如允许则平仓空头，并可选择开多。
   - `1 → 0`：如允许则平仓多头，并可选择开空。

区域分类（中性/多头/空头/极值）仅用于日志，与原版 EA 一样不会阻止交易决策。

## 参数说明

| 参数 | 描述 |
| --- | --- |
| `WilliamsPeriod` | Williams %R 的计算周期。 |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | 用于重建指标带的平滑成交量倍数。 |
| `SmoothingType` | 对加权值和成交量同时使用的移动平均类型（SMA、EMA、SMMA、WMA、Hull、VWMA、DEMA、TEMA）。 |
| `SmoothingLength` | 移动平均的长度。 |
| `SignalShift` | 读取完成颜色时向后偏移的 K 线数（1 对应原始默认值）。 |
| `EnableLongEntries` / `EnableShortEntries` | 允许开多/开空。 |
| `EnableLongExits` / `EnableShortExits` | 允许平多/平空。 |
| `VolumeSource` | 选择加权所用的成交量来源。 |
| `StopLossPoints` / `TakeProfitPoints` | 可选止损/止盈，单位为价格跳动。 |
| `CandleType` | 用于分析和交易的 K 线类型及周期。 |

仓位大小通过策略的 `Volume` 属性设置。触发反向信号时，策略会在现有仓位基础上加上配置的手数，与原始 EA 的
仓位管理一致。

## 使用提示

- MetaTrader 版本中的 `MA_Phase` 参数在 StockSharp 中不可用，因为内置移动平均不支持该设置。
- 请确保加载足够的历史数据，让移动平均在交易前已经形成。
- 策略可运行在任意支持的品种上；将 `CandleType` 设置为所需的时间框架（默认 4 小时）。
- 如果数据源不提供勾子数，请将 `VolumeSource` 切换为真实成交量。

## 日志与图表

策略会在默认图表区域绘制 K 线和 Williams %R 指标。交易日志会记录触发区域和当前平滑值，方便与 MetaTrader
原版进行对比和调试。
