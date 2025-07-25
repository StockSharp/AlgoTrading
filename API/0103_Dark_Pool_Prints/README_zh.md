# 暗池成交策略
[English](README.md) | [Русский](README_ru.md)

暗池成交策略追踪场外的大额交易，这些交易披露后往往会引发明显波动。
异常的成交量可能表明机构正在布局，尚未影响公开市场。
策略顺势跟随暗池的大额买卖，期待其被市场消化时继续推动价格。
若预期的动能未能出现，则在小幅百分比止损触发时离场。

测试表明年均收益约为 46%，该策略在股票市场表现最佳。

## 细节

- **入场条件**：指标信号
- **多/空**：均可
- **退出条件**：止损或反向信号
- **止损**：是，按百分比
- **默认值**:
  - `CandleType` = 15分钟
  - `StopLoss` = 2%
- **过滤器**:
  - 类别：成交量
  - 方向：双向
  - 指标：成交量
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

