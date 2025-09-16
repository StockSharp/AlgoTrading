# AfterEffects 策略
[English](README.md) | [Русский](README_ru.md)

AfterEffects 策略基于价格序列可能存在的滞后效应。
它利用当前收盘价以及 `p` 和 `2p` 根之前的开盘价计算信号：

`signal = Close - 2 * Open[p] + Open[2p]`

信号为正时开多，为负时做空。`Random` 参数会取反该信号。

进场后策略在入场价附近 `StopLoss` 点设置止损。
当价格向有利方向移动 `2 * StopLoss` 点时：

- 如果信号反转，策略以双倍仓位反向开仓；
- 否则，将止损跟随价格移动。

## 详情

- **入场条件**：`signal > 0` 做多，`signal < 0` 做空。
- **多空方向**：双向。
- **退出条件**：反向信号或止损。
- **止损**：跟踪止损。
- **默认值**:
  - `StopLoss` = 500
  - `Period` = 3
  - `Random` = false
  - `Volume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: 自定义公式
  - 止损: 跟踪
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
