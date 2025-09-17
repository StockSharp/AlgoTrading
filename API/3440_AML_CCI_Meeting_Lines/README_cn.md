# AML CCI Meeting Lines 策略
[English](README.md) | [Русский](README_ru.md)

该策略把 MetaTrader 5 智能交易系统 "Expert_AML_CCI" 移植到 StockSharp 的高级 API。当年的机器人将日本蜡烛形态
“Meeting Lines” 与商品通道指数（CCI）过滤器结合，并通过 Expert Advisor 引擎给多空信号赋予权重。移植版本保持相同
的确认逻辑，用纯粹的蜡烛运算重写形态识别，同时把所有阈值暴露为可优化的参数。

## 工作原理
* **数据源** – 策略通过 `SubscribeCandles` 订阅可配置的时间框架蜡烛（默认 30 分钟）。每根收盘蜡烛都会通过高级
  `Bind` 管道与同步的 CCI 数值一起传入，因此无需手动维护指标。
* **核心指标** – 单个 `CommodityChannelIndex` 指标（周期为 `CciPeriod`）完整复刻 MetaTrader 的振荡器。内部缓存存储
  最新两次收盘读数，用来重现 MQL 中的 `CCI(1)` 与 `CCI(2)` 访问方式。
* **蜡烛形态逻辑** – 辅助方法重新实现了 “Bullish Meeting Lines” 与 “Bearish Meeting Lines” 的检测。它会按 `AverageBodyPeriod`
  根蜡烛（默认 3）计算实体平均值，并检查长实体与相同收盘价的约束，与原始 `CML_CCI` 过滤器保持一致。因为
  StockSharp 只处理收盘蜡烛，形态会在第二根蜡烛收盘时立即评估，这与 MQL 智能交易系统给出 80 分投票的时刻相同。
* **入场规则** –
  * 多头：必须出现看涨 Meeting Lines 形态，并且最近一次 CCI 收盘值小于等于 `LongEntryCciLevel`（默认 −50）。若持有空头，
    下单数量会自动加上当前仓位的绝对值，从而实现翻仓，与原策略一致。
  * 空头：逻辑对称，需要看跌 Meeting Lines 形态以及最近 CCI 值大于等于 `ShortEntryCciLevel`（默认 +50）。
* **出场规则** – 移植版用显式平仓订单取代 Expert Advisor 的投票权重。当 CCI 穿越 `ExtremeCciLevel`（默认 80）定义的
  极值带时会退出：
  * 空头在 CCI 上穿 −Extreme 或重新跌破 +Extreme 时离场。
  * 多头在 CCI 跌破 +Extreme 或继续下穿 −Extreme 时离场。
  这些条件正是 MQL 信号类 `LongCondition` 和 `ShortCondition` 中权重为 40 的分支。
* **风险控制** – 策略本身不附带止损止盈，用户可以根据需要调用 StockSharp 的 `StartProtection` 工具来补充。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 蜡烛时间框架。 | 30 分钟 |
| `CciPeriod` | CCI 指标周期。 | 18 |
| `AverageBodyPeriod` | 计算实体平均长度所需的蜡烛数量。 | 3 |
| `LongEntryCciLevel` | 用于确认看涨形态的超卖阈值。 | −50 |
| `ShortEntryCciLevel` | 用于确认看跌形态的超买阈值。 | +50 |
| `ExtremeCciLevel` | CCI 穿越以触发退出的极值带。 | 80 |

所有数值型参数都带有与原 EA 相同的优化范围，方便在 StockSharp 的优化器中调整灵敏度或重新匹配资金管理方案。

## 使用提示
1. 启动前将策略绑定到目标证券，并设置合适的 `Volume`。
2. 如需复刻原始资金管理或调整信号灵敏度，可修改各类阈值参数。
3. 图表模块会绘制蜡烛、CCI 曲线以及成交点，便于快速确认形态识别是否与预期一致。

通过保持同样的蜡烛形态与 CCI 组合，这个 StockSharp 版本忠实地重现了 Expert_AML_CCI，同时完全遵循高层 API 的
推荐用法。
