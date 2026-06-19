# 楔形突破策略

## 概述

**楔形突破策略** 将 MetaTrader 专家顾问 *Wedge pattern.mq4* 迁移到了 StockSharp 高级 API。策略基于比尔·威廉姆斯分形识别对称楔形盘整，并在趋势与动能筛选同时满足时交易突破。

高阶实现保留了原始决策流程，同时使用 StockSharp 提供的管理功能：

- **趋势过滤器**：比较基于典型价格计算的快、慢线性加权移动平均（LWMA）。
- **动能过滤器**：评估 14 周期动能指标与 100 的距离，最近三次读数中至少一次必须超过阈值。
- **MACD 确认**：多头要求 MACD 主线高于信号线，空头则相反。
- **分形楔形检测**：收集上下分形构建收敛趋势线，收盘价突破并超出确认缓冲区时触发信号。
- **风险控制**：提供固定止损/止盈、自动保本（Break-even）以及跟踪止损，与 MQL 版本一致。

## 工作流程

1. 按照 `CandleType` 参数订阅单一时间框的 K 线。
2. 在每根完成的 K 线上更新指标，同时维护高低价缓冲区以侦测新分形。
3. 使用最新两个高分形与低分形构建楔形趋势线，仅在高点降低、低点抬升时认定为有效楔形。
4. 满足以下条件时开多：
   - 快速 LWMA 高于慢速 LWMA；
   - MACD 主线高于信号线；
   - 最近三次动能读数中任意一次超过阈值；
   - 收盘价突破上方趋势线并超过确认缓冲区。
5. 空头条件与上述相反。
6. 入场后立即设置止损与止盈，随后根据盈利情况自动移动至保本并启动跟踪止损。

## 参数说明

| 参数 | 含义 |
|------|------|
| `CandleType` | 使用的交易时间框。 |
| `FastMaPeriod` / `SlowMaPeriod` | 快慢 LWMA 的周期。 |
| `MomentumPeriod` | 动能指标的回溯期。 |
| `MomentumThreshold` | 认定动能有效所需的最小偏离量。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD 的标准配置。 |
| `FractalDepth` | 确认分形所需的左右柱数量。 |
| `StopLossPips` / `TakeProfitPips` | 初始止损与止盈（单位：点）。 |
| `UseBreakeven`、`BreakevenTriggerPips`、`BreakevenOffsetPips` | 保本功能开关及触发设置。 |
| `UseTrailing`、`TrailingActivationPips`、`TrailingDistancePips`、`TrailingStepPips` | 跟踪止损相关设置。 |
| `BreakoutBufferPips` | 突破确认缓冲距离。 |

所有与点值相关的设置都会根据交易品种的 `PriceStep` 自动转换为价格距离，能够兼容三位或五位小数的报价格式。

## 使用建议

1. 选择目标品种并设置 `CandleType` 为期望的时间框（例如 15 分钟）。
2. 通过 `Strategy.Volume` 调整仓位规模。
3. 根据市场波动性微调各项过滤与风险参数。
4. 启动策略后，它会自动订阅数据、绘制图表并在出现楔形突破时执行交易。

## 与 MQL 版本的差异

- 使用 `SubscribeCandles` 与指标绑定，避免逐笔处理。
- 止损、止盈、保本与跟踪功能通过 `SetStopLoss` / `SetTakeProfit` 实现，更易与内置风控集成。
- 仅保持单一仓位，不再逐单叠加最多 N 笔订单。
- 去除了原策略中的提示音、邮件和推送，相关通知可在外部实现。

在上述调整下，策略核心逻辑仍忠实复刻了原 MetaTrader 专家顾问，同时符合 StockSharp 的最佳实践。
