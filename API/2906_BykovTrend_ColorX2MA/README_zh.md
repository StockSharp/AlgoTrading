# BykovTrend + ColorX2MA 策略
[English](README.md) | [Русский](README_ru.md)

该策略将 BykovTrend V2 颜色趋势指标与 ColorX2MA 双重平滑均线斜率过滤器组合在一起。两个模块在同一交易品种上独立生成信号，最终仓位反映了二者的综合意见。

## 概览

- **适用市场**：任何提供蜡烛图数据的品种。默认时间框架为 H4，与原始 MT5 专家顾问一致。
- **指标**：
  - *BykovTrend V2*：根据 Williams %R 对蜡烛进行着色，区分多空趋势。
  - *ColorX2MA*：对选定价格进行两次移动平均平滑，通过第二次平滑的斜率判断趋势方向。
- **信号生成**：两个模块分别触发开仓/平仓，净头寸是所有成交的叠加结果。

## BykovTrend 模块

1. 使用设定周期（默认 9）的 Williams %R。
2. 门槛由 `33 - Risk` 调整；当 %R 高于 `-Risk` 判定为多头趋势，低于 `-100 + (33 - Risk)` 判定为空头趋势。
3. 颜色编码：0、1 表示多头；2 为中性；3、4 表示空头。
4. 信号基于 `SignalBar` 个已完成的蜡烛，值为 1 时表示上一根闭合蜡烛，与 MT 实现保持一致。
5. 交易规则：
   - **做多开仓**：当前颜色 < 2 且上一颜色 > 1（从中性/空头转为多头）。
   - **平空**：当前颜色 < 2。
   - **做空开仓**：当前颜色 > 2 且上一颜色 < 3（从中性/多头转为空头）。
   - **平多**：当前颜色 > 2。
   - 可分别通过 *Bykov Allow Long/Short Entries/Exits* 参数启用或关闭。

## ColorX2MA 模块

1. 第一层均线按照选择的方法和周期对价格进行平滑。
2. 第二层均线再次平滑第一层输出。
3. 斜率决定颜色：1 表示上涨，2 表示下跌，0 表示持平。
4. 信号同样使用 `SignalBar` 参数控制延迟，默认取上一根闭合蜡烛。
5. 交易规则：
   - **做多开仓**：颜色由非 1 变为 1。
   - **平空**：当前颜色 = 1。
   - **做空开仓**：颜色由非 2 变为 2。
   - **平多**：当前颜色 = 2。
   - 通过 *Color Allow Long/Short Entries/Exits* 参数管理权限。

## 仓位管理

- 全部使用市价单。反向操作时会下单 `Volume + |Position|` 的数量，以先平掉原有仓位再建立新仓。
- 两个模块可以单独触发平仓，因此可能出现信号博弈的情况。
- 策略本身不包含止损/止盈，需要外部风控或合理设置许可参数。

## 参数说明

| 参数 | 说明 |
|------|------|
| **BykovTrend Candle** | BykovTrend 使用的时间框架。 |
| **Williams %R Period** | Williams %R 的回溯周期。 |
| **Risk Offset** | 调整 %R 门槛的风险因子 `33 - Risk`。 |
| **Signal Bar** | 处理信号前需要等待的已完成蜡烛数量。 |
| **Allow Long/Short Entries** | 是否允许 BykovTrend 开多/开空。 |
| **Allow Long/Short Exits** | 是否允许 BykovTrend 平多/平空。 |
| **ColorX2MA Candle** | ColorX2MA 使用的时间框架。 |
| **First/Second MA Method** | 两层均线的平滑方法（SMA、EMA、SMMA、LWMA、Jurik）。 |
| **First/Second MA Length** | 两层均线的周期。 |
| **First/Second MA Phase** | 从原版保留的相位参数，当前实现不影响 StockSharp 内置指标。 |
| **Applied Price** | 使用的价格类型（收盘价、开盘价、高/低价、中值、典型价、加权价、简单平均、Quarter、TrendFollow、DeMark）。 |
| **Color Signal Bar** | ColorX2MA 信号延迟。 |
| **Allow Long/Short Entries/Exits** | 是否允许 ColorX2MA 模块执行对应动作。 |

## 注意事项

- 仅支持 StockSharp 提供的均线类型；MetaTrader 附加的 JurX、Parabolic、T3、VIDYA、AMA 等算法未实现。
- 相位参数仅用于兼容文档，不会改变内置指标的计算。
- 请确保设置策略的 `Volume`，否则不会下单。
- 在 MT 平台中通过 magic number 区分仓位，本策略则将所有成交合并成净仓，因此结果可能与 MT 版不同。
