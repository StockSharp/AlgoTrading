# Book to Market Value
[English](README.md) | [Русский](README_ru.md)

**Book-to-Market Value** 策略展示了如何设置交易品种列表并订阅日线K线以支持 book-to-market 因子。
当前实现仅为示例，不包含具体交易逻辑。

## 详情
- **入场条件**：因子逻辑未实现。
- **多空方向**：双向。
- **退出条件**：无。
- **止损**：无。
- **默认值**:
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **过滤器**:
  - 分类: 基本面
  - 方向: 双向
  - 指标: Fundamentals
  - 止损: 否
  - 复杂度: 低
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 低
