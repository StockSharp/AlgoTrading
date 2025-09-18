# TCPivotStop 策略

## 概述

TCPivotStop 策略直接移植自 MetaTrader 4 智能交易系统 `gpfTCPivotStop`。它围绕上一交易日计算出的经典枢轴点进行交易，核心逻辑如下：

- 根据上一日的最高价、最低价和收盘价计算枢轴点以及三层支撑/阻力位。
- 当当前收盘价向上突破枢轴点时做多，向下跌破时做空。
- 按突破方向开仓，并将止损、止盈设置在所选的枢轴层级。
- 可选地在指定的交易时段开始时强制平仓，以复现原始策略的日内退出机制。

实现基于 StockSharp 高级 API，仓位规模使用基类 `Strategy` 的 `Volume` 属性控制。

## 参数

| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `TargetLevel` | 用作止损/止盈的枢轴层级（1、2 或 3）。 | `1` |
| `CloseAtSessionStart` | 启用后，在设置的小时开始时平掉持仓。 | `false` |
| `SessionCloseHour` | 配合 `CloseAtSessionStart` 使用的小时（0-23）。 | `0` |
| `CandleType` | 产生交易信号的K线周期。 | `H1` |

## 交易逻辑

1. 订阅配置周期的K线作为信号源，同时订阅日线用于枢轴计算。
2. 每根日线收盘后计算经典枢轴：
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 * Pivot - Low`，`S1 = 2 * Pivot - High`
   - `R2 = Pivot + (R1 - S1)`，`S2 = Pivot - (R1 - S1)`
   - `R3 = High + 2 * (Pivot - Low)`，`S3 = Low - 2 * (High - Pivot)`
3. 每根信号K线收盘时：
   - 若启用 `CloseAtSessionStart` 且开盘时间等于 `SessionCloseHour`，立即平仓。
   - 若空仓且上一根收盘价在枢轴下方、当前收盘价上穿枢轴，则做多并应用所选层级的止损/止盈。
   - 若空仓且上一根收盘价在枢轴上方、当前收盘价下破枢轴，则做空并应用镜像的止损/止盈。
   - 若已有持仓，当收盘价触及相应止损或止盈时平仓。

## 备注

- 调用 `StartProtection()` 可以接入平台的风险控制模块，具体的止损/止盈判定在策略内部完成。
- 原始 MT4 版本包含邮件通知和基于风险的浮动手数，本移植版本未包含此功能。如有需要，请结合 StockSharp 的通知及资金管理组件使用。
- 原策略的 `isTradeDay` 选项在午夜平仓，可通过 `CloseAtSessionStart` 与 `SessionCloseHour=0` 的组合复现。
