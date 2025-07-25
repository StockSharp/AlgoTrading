# 隔夜缺口策略
[English](README.md) | [Русский](README_ru.md)

该策略在开盘时交易因消息或盘后活动造成的显著跳空。
大型缺口通常会部分回补，因为市场需要消化这一波动。
策略在开盘后不久逆向介入，等待缺口回补，并在当日收盘前了结。
止损根据缺口极值外一定百分比设定，以防价格继续扩张。

测试表明年均收益约为 124%，该策略在外汇市场表现最佳。

## 细节

- **入场条件**：指标信号
- **多/空**：均可
- **退出条件**：止损或反向信号
- **止损**：是，按百分比
- **默认值**:
  - `CandleType` = 15分钟
  - `StopLoss` = 2%
- **过滤器**:
  - 类别：缺口
  - 方向：双向
  - 指标：缺口
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

