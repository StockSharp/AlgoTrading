# Exp Blau HLM 策略

## 概述

**Exp Blau HLM** 是 MetaTrader 5 专家顾问 `Exp_BlauHLM.mq5` 的 StockSharp 版本。策略使用 Blau High-Low Momentum (HLM) 振荡指标，对比最近的高低点、通过 XMA 级联进行平滑，并支持三种原始模式：

- **Breakdown**：利用直方图穿越零轴的信号。
- **Twist**：监控直方图斜率的“扭转”来捕捉动量变化。
- **CloudTwist**：比较上/下包络线的交叉，类似云图信号。

本移植保留了原始参数和交易逻辑，仓位大小通过基础策略的 `Volume` 属性控制。

## 交易逻辑

1. 每当所选周期的蜡烛收盘时，计算 Blau HLM 指标：
   - 取得当前最高价与 `XLength - 1` 根之前最高价的差值，以及最低价的镜像差值。
   - 将负值截断为零，相减后得到原始 HLM（若合约提供 `PriceStep`，则以点值表示）。
   - 依次通过四个相同类型、不同周期的移动平均进行平滑。
2. 根据 **EntryMode**：
   - **Breakdown**：当较早的直方图大于零而较新的值不大于零时触发多头，并在同样条件下平掉空头；反向条件触发空头/平多。
   - **Twist**：比较三个历史点的斜率，若中间值在此前下降后再次上升，则视为看多扭转；反之为看空扭转。
   - **CloudTwist**：监控上下通道，若旧值上轨高于下轨且新值发生反向穿越，则发出多/空信号。
3. `BuyOpen`、`SellOpen`、`BuyClose`、`SellClose` 控制开平仓权限。遇到相反信号时，会先平掉当前仓位，再按 `Volume` 发送市价单。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `CandleType` | `DataType` | H4 蜡烛 | 用于分析的时间框架。 |
| `SmoothingMethod` | `SmoothMethod` | `Exponential` | XMA 平滑方式（不支持的旧模式会回退到 EMA）。 |
| `XLength` | `int` | `2` | 计算高低差值的窗口长度。 |
| `FirstLength` | `int` | `20` | 第一次平滑的周期。 |
| `SecondLength` | `int` | `5` | 第二次平滑的周期。 |
| `ThirdLength` | `int` | `3` | 第三次平滑的周期。 |
| `FourthLength` | `int` | `3` | 最终信号平滑的周期。 |
| `Phase` | `int` | `15` | Jurik 相位（限制在 ±100，仅对 Jurik 生效）。 |
| `SignalBar` | `int` | `1` | 生成信号时引用的历史偏移量。 |
| `EntryMode` | `Mode` | `Twist` | 交易模式（`Breakdown`、`Twist`、`CloudTwist`）。 |
| `BuyOpen` / `SellOpen` | `bool` | `true` | 是否允许开多/开空。 |
| `BuyClose` / `SellClose` | `bool` | `true` | 是否允许在反向信号时平多/平空。 |

## 移植说明

- MQL 库 `SmoothAlgorithms.mqh` 提供了 JJMA、JurX、ParMA、T3、VIDYA、AMA 等专有平滑算法。StockSharp 仅内置常见版本，未实现的选项使用 EMA 近似。
- 原策略中的资金管理参数 (`MM`、`MarginMode`、`StopLoss`、`TakeProfit`、`Deviation`) 在此版本中由 `Volume` 和市价执行取代。
- `SignalBar` 的行为被完整保留：策略维护内部缓冲区，始终对比指定偏移的历史值，以便于与 MT5 回测结果对齐。
- 保护逻辑由 `StartProtection()` 启动，如需固定止损/止盈，请在上层策略或连接器中配置。

## 使用建议

1. 在启动策略前设置 `Volume`，确定每笔交易的合约数量。
2. 对无有效 `PriceStep` 的品种，指标以原始价格为单位，必要时调整平滑周期以匹配波动。
3. 使用非指数平滑时避免在很短周期上使用极端 `Phase` 值，否则信号会非常噪声。可适当增大周期。
4. 建议结合账户级风险控制或其他保护措施来模拟原策略中的止损/止盈逻辑。

