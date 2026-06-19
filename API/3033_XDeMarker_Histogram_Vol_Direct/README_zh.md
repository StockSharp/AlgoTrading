# XDeMarker Histogram Vol Direct 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 MetaTrader 5 中的 **Exp_XDeMarker_Histogram_Vol_Direct** 专家移植到 StockSharp 平台。它把 DeMarker 指标与选定的
成交量流（tick 数或真实成交量）相乘，再用相同的移动平均对指标与成交量进行平滑处理，并与可调的上下动态区间进行比较。
当平滑后的柱状图在相邻两根柱之间改变方向时触发交易信号。

## 指标流程

1. 在配置的时间框架上计算经典的 DeMarker 振荡指标。
2. 将每根已完成 K 线的 DeMarker 值乘以 tick 数或真实成交量。
3. 使用所选移动平均类型同时平滑柱状图和成交量。
4. 用平滑成交量乘以上下阈值系数，得到四条动态带。
5. 根据柱状图方向（上升/下降）判断是否需要翻转头寸。当方向翻转时，关闭反向仓位并按新方向建立仓位。

目前支持的平滑方式包括简单、指数、平滑（RMA/SMMA）以及线性加权移动平均。原 MQL 版本中的 JJMA、JurX、ParMA、
T3、VIDYA、AMA 等特殊滤波在本移植中未实现。

## 交易规则

- **做多开仓**：`Allow Long Entry = true` 时启用。如果前一根柱的方向为“上”，而最新柱转为“下”，策略将持有 `Volume`
  数量的多头。
- **做空开仓**：`Allow Short Entry = true` 时启用。条件相反：前一柱“下”，当前柱“上”。
- **多头平仓**：`Allow Long Exit = true` 且上一柱方向为“下”时将平掉多头（除非同一根柱出现新的做空信号）。
- **空头平仓**：`Allow Short Exit = true` 且上一柱方向为“上”时将平掉空头。

每根完成的 K 线仅计算一次信号。保持了原策略一根 K 线的延迟；`Signal Bar` 参数仅用于兼容性，当设置为非 `1` 值时会
给出警告并忽略。

## 参数说明

| 参数 | 说明 |
|------|------|
| Candle Type | 生成指标所用的 K 线类型（时间框架）。 |
| DeMarker Period | DeMarker 基础周期。 |
| Volume Source | 使用 tick 数还是真实成交量。 |
| High Level 2 / High Level 1 | 乘以平滑成交量得到的上方阈值。 |
| Low Level 1 / Low Level 2 | 下方阈值。 |
| Smoothing Method | 对柱状图和成交量使用的移动平均类型。 |
| Smoothing Length | 平滑窗口长度。 |
| Smoothing Phase | 兼容性占位参数，在该版本中未使用。 |
| Signal Bar | 历史偏移，固定为 1。 |
| Allow Long/Short Entry | 是否允许开多/开空。 |
| Allow Long/Short Exit | 是否允许自动平多/平空。 |

## 实现细节

- 自定义的 `XDeMarkerHistogramVolDirectIndicator` 复刻了 MT5 指标缓冲区，通过复杂指标值提供平滑柱状图、动态水平以及方向。
- 当需要改变目标仓位时，策略只发送一笔市价单，将当前仓位调整为 `Volume`、`-Volume` 或 0，从而复制原脚本中“先平再开”的
  行为并避免重复下单。
- 如果图表区域可用，会自动绘制 K 线、指标曲线以及成交记录，便于可视化分析。
