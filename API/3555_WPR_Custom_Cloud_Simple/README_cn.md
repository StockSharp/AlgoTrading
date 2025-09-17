# WPR Custom Cloud Simple 策略

## 概述
**WPR Custom Cloud Simple 策略** 是 MetaTrader 智能交易程序 `WPR Custom Cloud Simple.mq5` 的 StockSharp 版本。原策略利用 Larry Williams 的 %R 摆动指标，在指标脱离超买或超卖区域时开仓。本 C# 实现保持了原始 EA 的节奏：只在新 K 线出现时评估信号、收到反向信号时直接反手，并且不使用止损、止盈或跟踪保护。

## 交易逻辑
1. 订阅所选时间框架 (`CandleType`)，并用收到的蜡烛数据驱动 `WilliamsR` 指标。
2. 仅在蜡烛收盘后处理，未完成的 K 线不会触发交易。
3. 保存最近两根已完成蜡烛的 %R 数值，对应 MetaTrader 中的 `wpr[1]` 与 `wpr[2]`。
4. 当出现交叉时发出信号：
   - **做多**：上一根蜡烛的 %R 收盘价高于 `OversoldLevel`，而再前一根蜡烛低于该阈值，重现 EA 中“从超卖区域向上突破”的条件。
   - **做空**：上一根蜡烛的 %R 收盘价低于 `OverboughtLevel`，而再前一根蜡烛高于该阈值，对应 EA 的超买向下突破判断。
5. 做多时先平掉所有空头净头寸，再按策略基准手数买入；做空时则先平掉多头净头寸再卖出。StockSharp 使用净头寸模型，通过 `Volume + |Position|` 的下单量即可复刻 MetaTrader 中先平仓再反手的流程。
6. 无额外离场机制；只有新的反向交叉才会关闭现有仓位，与原策略完全一致。

## 参数
| 名称 | 类型 | 默认值 | MetaTrader 对应参数 | 说明 |
| --- | --- | --- | --- | --- |
| `WprPeriod` | `int` | `14` | `Inp_WPR_Period` | Williams %R 的计算周期。 |
| `OverboughtLevel` | `decimal` | `-20` | `Inp_WPR_Level1` | 定义超买区域的阈值，向下突破后触发做空。 |
| `OversoldLevel` | `decimal` | `-80` | `Inp_WPR_Level2` | 定义超卖区域的阈值，向上突破后触发做多。 |
| `CandleType` | `DataType` | 1 小时时间框架 | `InpWorkingPeriod` | 驱动指标与信号评估的 K 线类型。 |
| `Volume` | `decimal` | 策略基准手数 | `InpLots` | 市价单的下单量；策略会在入场前自动抵消当前净头寸。 |

## 与原 EA 的差异
- StockSharp 采用净持仓模式，通过调整市价单数量即可在一次请求中实现平仓与反手，不需要原代码中的 `STRUCT_POSITION` 或复杂的队列管理。
- 原 EA 中的 `CTrade`、`CPositionInfo`、保证金检查等辅助类被 StockSharp 自带的风控功能替代。策略直接依赖 `Strategy.Volume` 与交易所元数据来确保下单合法。
- 日志输出大幅精简。高层 API 已经提供订单状态回报，因此无需重复 `Print` 调试信息。
- 为忠实还原“反向信号离场”的理念，策略没有实现任何止损、止盈或跟踪保护。

## 使用建议
- 将 `CandleType` 设置为与 MetaTrader 相同的时间框架，以获得相近的信号频率。
- Williams %R 阈值为负数。`OverboughtLevel` 越接近 0，做空信号越少；`OversoldLevel` 越接近 `-100`，做多信号越少。
- 请确保 `Volume` 与交易标的的最小交易单位、步长一致。在实盘前可通过 UI 或代码调整基准手数。
