# Beta Weighted MA策略
[English](README.md) | [Русский](README_ru.md)

Beta Weighted MA（BWMA）策略使用Beta分布对最近的价格加权，通过alpha和beta参数调节滞后和平滑程度。当价格上穿BWMA时开多仓，下穿时开空仓。

## 细节

- **入场条件**：
  - 价格上穿BWMA时做多。
  - 价格下穿BWMA时做空。
- **方向**：多/空。
- **出场条件**：
  - 反向穿越关闭当前仓位并开立反向仓位。
- **止损**：无。
- **默认参数**：
  - `Length` = 50
  - `Alpha` = 3
  - `Beta` = 3
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多/空
  - 指标：Beta Weighted Moving Average
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
