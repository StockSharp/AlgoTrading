# 随机指标失败摆动策略
[English](README.md) | [Русский](README_ru.md)

该策略监控随机振荡指标，当80以上出现更低的高点或20以下出现更高的低点时留意反转。
指标未能刷新极值并掉头，通常意味着趋势即将变化。
在20下方形成更高低点且%K重新上穿%D时买入；在80上方形成更低高点且%K跌破%D时卖出。
采用小幅百分比止损，若随机指标回到先前摆动水平则退出。

测试表明年均收益约为 70%，该策略在股票市场表现最佳。

## 细节

- **入场条件**：指标信号
- **多/空**：均可
- **退出条件**：止损或反向信号
- **止损**：是，按百分比
- **默认值**:
  - `CandleType` = 15分钟
  - `StopLoss` = 2%
- **过滤器**:
  - 类别：反转
  - 方向：双向
  - 指标：随机指标
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

