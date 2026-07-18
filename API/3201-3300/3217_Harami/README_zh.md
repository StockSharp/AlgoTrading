# Harami 策略

## 概述
HaramiStrategy 将 MetaTrader 的 “Harami” 专家顾问转换为 StockSharp 的高级 API 实现。策略在较高周期上寻找多头/空头的孕线形态，并通过动量扩张与长期 MACD 过滤器进行确认。系统只处理收盘后的完整K线，并使用 StockSharp 的保护模块管理风险。

## 数据与指标
- **基础周期：** 可配置（默认 15 分钟），用于移动平均趋势判断。
- **高阶周期：** 可配置（默认 1 小时），用于形态识别与动量确认。
- **MACD 周期：** 可配置（默认 30 天 K 线），模拟原策略使用的月线 MACD 过滤。
- **指标组合：**
  - 基础周期上的线性加权移动平均（`FastMaLength`）。
  - 基础周期上的指数移动平均（`SlowMaLength`）。
  - 高阶周期上的动量指标（`MomentumPeriod`）。策略记录最近三根高阶K线动量值与 100 的绝对偏离量。
  - MACD（12/26/9 组合）应用在 MACD 周期上。

## 多头条件
1. 基础周期的慢速 EMA 高于快速 LWMA，说明趋势向上。
2. 高阶周期出现看涨孕线：前一根K线为阳线，倒数第二根为阴线，并且当前阳线实体更小。
3. 最近三根高阶K线中任意一个动量偏离值大于 `MomentumBuyThreshold`。
4. MACD 主线位于信号线上方。
5. 当前没有多头持仓（`Position <= 0`）。
6. 策略发送市价买单，先平掉所有空头，再新增 `Volume` 手多头。

## 空头条件
1. 基础周期的慢速 EMA 低于快速 LWMA。
2. 高阶周期出现看跌孕线：倒数第二根为阳线，上一根为阴线，并且阴线实体更小。
3. 最近三根高阶K线中任意一个动量偏离值大于 `MomentumSellThreshold`。
4. MACD 主线位于信号线下方。
5. 当前没有空头持仓（`Position >= 0`）。
6. 策略发送市价卖单，先平掉所有多头，再建立 `Volume` 手空头。

## 风险控制
`StartProtection` 会按照点数安装止损和止盈。原始 EA 中的追踪止损、保本和资金管理逻辑为保持简洁而未在本版本实现。反向信号会自动对冲并翻转仓位。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `CandleType` | 信号执行与均线计算的基础周期。 | 15 分钟 |
| `HigherCandleType` | 孕线与动量确认所用的高阶周期。 | 1 小时 |
| `MacdCandleType` | MACD 过滤器的周期。 | 30 天 |
| `FastMaLength` | 快速线性加权均线长度。 | 6 |
| `SlowMaLength` | 慢速指数均线长度。 | 85 |
| `MomentumPeriod` | 高阶周期动量回溯长度。 | 14 |
| `MomentumBuyThreshold` | 多头确认所需的最小动量偏离。 | 0.3 |
| `MomentumSellThreshold` | 空头确认所需的最小动量偏离。 | 0.3 |
| `StopLossPoints` | 止损距离（点）。 | 40 |
| `TakeProfitPoints` | 止盈距离（点）。 | 100 |

## 使用建议
- 根据可用历史数据协调 `CandleType`、`HigherCandleType` 与 `MacdCandleType`，确保高阶周期长于基础周期。
- 按照交易品种波动性调整动量阈值。
- 借助 StockSharp 的优化器，在给定范围内优化均线长度与动量阈值。
- 在真实交易前务必结合手续费与滑点进行充分回测。
