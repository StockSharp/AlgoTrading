# Nadaraya-Watson Envelope 策略
[English](README.md) | [Русский](README_ru.md)

构建对数尺度的Nadaraya-Watson核回归包络。价格向上突破下包络时做多，若选择长短双向模式，价格向下突破上包络时做空。

## 详情

- **入场条件**：
  - 收盘价向上穿越下包络做多。
  - 收盘价向下穿越上包络做空（长短双向模式）。
- **多空方向**：可配置。
- **出场条件**：相反包络的交叉。
- **止损**：无。
- **默认值**：
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Envelope
  - 方向：可配置
  - 指标：Nadaraya-Watson
  - 止损：无
  - 复杂度：高级
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
