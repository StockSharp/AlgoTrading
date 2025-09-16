# Wajdyss Ichimoku Candle MMRec 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
本策略基于 MetaTrader 专家顾问 *Exp_wajdyss_Ichimoku_Candle_MMRec* 改写而成。它重新计算 Ichimoku 的基准线 Kijun，利用 `Highest`
和 `Lowest` 指标获取指定周期内的最高价和最低价，然后为每根收盘蜡烛赋予 4 种颜色之一。当较早的蜡烛位于 Kijun 之上而最新的信号
蜡烛跌破 Kijun 时，策略会平掉空头并考虑做多；相反的颜色切换会触发平多并进入空头。MMRec 模块会在连续出现指定数量的亏损后
把下单手数降低到防御水平。

移植版本完全使用 StockSharp 高级 API，通过一次 `SubscribeCandles` 订阅即可获取行情，所有判断均在蜡烛收盘后执行，以便在回测
和实时环境中保持一致。

## 蜡烛着色规则
| 颜色 | 条件 | 含义 |
|------|------|------|
| `0` | 收盘价低于 Kijun 且是阴线 | 强烈的空头动能 |
| `1` | 收盘价低于 Kijun 但为阳线 | 低位反弹 |
| `2` | 收盘价高于 Kijun 但为阴线 | 高位回落 |
| `3` | 收盘价高于 Kijun 且为阳线 | 强劲的多头延续 |

## 信号逻辑
- **做多**：`SignalBarShift + 1` 位置的颜色大于 `1`（价格位于 Kijun 之上），而 `SignalBarShift` 位置的颜色小于 `2`
  （价格跌到 Kijun 下方）。策略可以选择平掉空头并开多。
- **做空**：`SignalBarShift + 1` 位置的颜色小于 `2`，而 `SignalBarShift` 位置的颜色大于 `1`。策略在需要时平掉多头并开空。

`SignalBarShift` 与原始 EA 中的 `SignalBar` 参数一致，默认值 `1` 表示使用最近两根已完成的蜡烛。增大该值会延迟进场。

## 资金管理
策略分别为多头和空头保存最近的交易结果。如果最近 `LossTriggerCount` 笔同方向交易全部亏损，下单手数会切换为 `ReducedVolume`；
一旦出现盈利或者历史交易数量不足，就恢复到 `NormalVolume`。该机制对应 MQL 库中的 `BuyTradeMMRecounter` 与
`SellTradeMMRecounter` 函数。

## 风险管理
止损与止盈使用价格步长表示。例如多头持仓时检查是否触及 `开仓价 - StopLossPoints * PriceStep` 或
`开仓价 + TakeProfitPoints * PriceStep`，空头逻辑正好相反。检查在每根蜡烛收盘时执行，与原策略使用固定距离的服务器挂单一致。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于计算指标的蜡烛周期 | 1 小时 |
| `KijunLength` | Kijun 计算窗口 | 26 |
| `SignalBarShift` | 参与信号判断的蜡烛位移 | 1 |
| `BuyPosOpen` / `SellPosOpen` | 是否允许开仓 | `true` |
| `BuyPosClose` / `SellPosClose` | 是否允许因反向信号平仓 | `true` |
| `NormalVolume` | 默认下单手数 | `1` |
| `ReducedVolume` | 连续亏损后的下单手数 | `0.1` |
| `LossTriggerCount` | 触发缩减手数所需的亏损次数 | `2` |
| `StopLossPoints` | 止损距离（价格步长，`0` 表示关闭） | `1000` |
| `TakeProfitPoints` | 止盈距离（价格步长，`0` 表示关闭） | `2000` |

## 使用提示
- 仅当颜色发生翻转且对应方向被启用时才会开仓。
- 资金管理依赖于策略生成的成交结果，在回测中这些结果会自动更新统计。
- 如果标的物没有定义价格步长，止损与止盈设置会被忽略。
- 将 `SignalBarShift` 设为 `0` 可以更快响应最新蜡烛，但可能带来更多噪音信号。
