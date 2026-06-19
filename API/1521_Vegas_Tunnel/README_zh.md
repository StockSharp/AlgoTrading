# Vegas Tunnel 策略
[English](README.md) | [Русский](README_ru.md)

使用四条 EMA 构成隧道，并可选择基于 ATR 的止损。
当价格和快 EMA 高于慢 EMA 与隧道 EMA 时做多，反之做空。

## 细节

- **入场条件**: EMA 与价格相对于隧道的位置
- **多空方向**: 双向
- **出场条件**: 止损或止盈
- **止损**: ATR 或 EMA 基础
- **默认值**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: EMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
