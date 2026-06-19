# Alligator 波动率策略

Alligator 波动率策略是对 MetaTrader "Alligator vol 1.1" 专家顾问的 StockSharp 高级移植版本。策略利用比尔·威廉姆斯的 Alligator 指标，并提供可选的分形突破过滤、马丁格尔加仓网格以及跟踪止损管理，帮助交易者自动化原有流程。

## 逻辑概览

- 订阅设定的 K 线数据，并计算 Alligator 的三条平滑移动平均线（下颌、牙齿、嘴唇）。
- 当嘴唇高于下颌且间距不少于 `EntryGap`，并且仍高于牙齿 `ExitGap` 时视为多头阶段；反之则判定为空头阶段。
- 在最近 `FractalBars` 根已完成 K 线上寻找标准五根 K 线分形，可选的分形过滤要求多头突破最新上分形、空头突破最新下分形。
- 当 Alligator 状态出现新的多头/空头信号时开仓；若启用马丁格尔，则根据止损距离分层布置加仓限价单，仓位按照指数方式放大。
- 使用止损、止盈、可选的跟踪止损以及可选的 Alligator 反向信号管理离场。

## 入场规则

1. 仅处理状态为 `Finished` 的完整 K 线。
2. 多头条件：
   - 启用 Alligator 入场时，状态由非多头切换为多头，并且（若启用）最近上分形高出当前收盘价至少 `FractalDistancePips`。
   - 关闭 Alligator 入场时，只要满足分形过滤（若启用）即可。
3. 空头条件与多头对称，使用下分形验证。
4. `ManualMode` 为真时禁用自动开仓，可用于手动干预。
5. `OnlyOnePosition` 为真时，若已经存在持仓则不会再开同向新仓。

## 离场规则

- 开仓成交后立即根据平均持仓价设置止损和止盈，距离由 `StopLossPips` 与 `TakeProfitPips` 换算成价格。
- `EnableTrailing` 为真时，盈利至少达到 `TrailingActivationPips` 后启动跟踪止损，多单跟随最高价，空单跟随最低价。
- `UseAlligatorExit` 为真时，当 Alligator 状态回落（多头消失或空头消失）将立即平仓。
- 触发止盈或止损后会同时取消对应方向的挂单加仓。

## 马丁格尔网格

- `EnableMartingale` 启用后，会在市场单之后挂出一组限价加仓单。
- 每一级别的下单量等于上一笔已成交量乘以 `2 * MartingaleMultiplier`，并受 `MaxVolume` 限制。
- 限价价位以止损距离 (`StopLossPips`) 为步长，并可通过 `GridSpreadPips` 补偿点差。
- 在信号刷新、仓位被平掉或手动退出时会自动取消未成交的网格订单。

## 资金管理

- 开仓量按账户权益计算：`volume = equity / 1000 * RiskPerThousand`。
- 当无法获取权益时使用 `MinVolume` 作为保底值，并通过 `MaxVolume` 限制最大下单量。
- 所有下单价格会根据交易所最小变动价位进行四舍五入。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 使用的 K 线数据类型。 | 15 分钟 |
| `ManualMode` | 为真时禁用自动入场。 | `false` |
| `UseAlligatorEntry` | 是否要求 Alligator 扩张信号。 | `true` |
| `UseFractalFilter` | 是否启用分形突破过滤。 | `false` |
| `UseAlligatorExit` | Alligator 收口时是否强制离场。 | `false` |
| `OnlyOnePosition` | 是否只允许单一持仓。 | `true` |
| `EnableMartingale` | 是否挂出马丁格尔网格。 | `true` |
| `EnableTrailing` | 是否启用跟踪止损。 | `true` |
| `RiskPerThousand` | 每 1000 单位权益对应的下单系数。 | `0.04` |
| `MaxVolume` | 最大下单量。 | `0.5` |
| `MinVolume` | 最小下单量。 | `0.01` |
| `StopLossPips` / `TakeProfitPips` | 止损与止盈距离（点数）。 | `80` |
| `TrailingStopPips` | 跟踪止损距离（点数）。 | `30` |
| `TrailingActivationPips` | 启动跟踪止损所需盈利（点数）。 | `20` |
| `EntryGap` | 嘴唇与下颌的最小价差（价格单位）。 | `0.0005` |
| `ExitGap` | 嘴唇或下颌与牙齿的最小价差（价格单位）。 | `0.0001` |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | Alligator 平滑平均周期。 | `13 / 8 / 5` |
| `JawShift` / `TeethShift` / `LipsShift` | 计算信号时的偏移条数。 | `8 / 5 / 3` |
| `FractalBars` | 搜索分形的历史 K 线数量。 | `10` |
| `FractalDistancePips` | 分形与价格的最小距离（点数）。 | `30` |
| `MartingaleDepth` | 马丁格尔挂单层数。 | `10` |
| `MartingaleMultiplier` | 加仓量外加的倍数系数。 | `1.3` |
| `GridSpreadPips` | 网格价位的点差补偿。 | `10` |

## 说明

- Alligator 指标以 K 线中值计算，并使用一根 K 线延迟来避免未完成数据。
- `EntryGap` 与 `ExitGap` 以绝对价格表示，请结合品种最小价差调整。
- 分形识别基于标准五根 K 线模型，启用过滤后需等待足够历史数据。
- 策略内部管理止损与止盈，不会向交易所提交保护单。
- 若用户手动修改挂单或仓位，策略会在订单状态变化时自动同步并清理内部网格。
