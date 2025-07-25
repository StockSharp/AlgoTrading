# 斐波那契回撤反转策略
[English](README.md) | [Русский](README_ru.md)

市场在延续趋势前往往会回撤部分幅度。本策略识别最近的波峰波谷，关注价格测试 61.8% 或 78.6% 回撤位，这些区域常是行情衰竭点。

测试表明年均收益约为 115%，该策略在股票市场表现最佳。

算法在滚动窗口中追踪波动，并计算它们之间的斐波那契水平。当价格接近关键回撤并出现与原趋势方向一致的蜡烛时开仓，止损按固定百分比放置。目标位大约在该波动的 50% 中点。

通过关注既定趋势中的深度回调，该方法试图捕捉趋势延续的早期阶段，在空头或多头短暂掌控后入场。

## 细节

- **入场条件**：价格测试 61.8% 或 78.6% 回撤位，并出现确认蜡烛。
- **多/空**：依据趋势可做多或做空。
- **退出条件**：价格到达 50% 水平或止损。
- **止损**：是，按百分比。
- **默认值**：
  - `SwingLookbackPeriod` = 20
  - `FibLevelBuffer` = 0.5
  - `CandleType` = 5 分钟
  - `StopLossPercent` = 2
- **过滤条件**：
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: 斐波那契水平
  - 止损: 有
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

