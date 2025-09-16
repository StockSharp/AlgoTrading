# Bollinger Band Two MA ZigZag 策略
[English](README.md) | [Русский](README_ru.md)

该混合趋势策略结合了布林带反转、两个高周期均线以及 ZigZag 枢轴点。每次出现信号时会开启两笔仓位：第一笔按照计算出的止盈目标离场，第二笔作为“趋势持仓”依靠保本与移动止损进行管理。

## 细节

- **入场条件**：
  - **做多**：上一根 K 线在此前两根跌破下轨后重新收在前一根下轨之上，当前收盘价也高于该下轨，同时价格位于两个高周期均线上方。
  - **做空**：上一根 K 线在此前两根突破上轨后重新收在前一根上轨之下，当前收盘价也低于该上轨，同时价格位于两个高周期均线下方。
- **仓位管理**：
  - 每次信号都会以 `First Volume`（带止盈）和 `Second Volume`（趋势持仓）两个独立手数开仓。
  - 止损基于最近一次 ZigZag 枢轴点并加减 `Pivot Offset (pts)`。
  - 启用保本功能时，当浮盈达到 `Break-even Threshold (pts)` + `Break-even Offset (pts)` 时，止损移动到入场价加上 `Break-even Offset (pts)`。
  - 当价格相对当前止损继续运行 `Trailing Step (pts)` 时，移动止损保持与价格的距离为 `Trailing Stop (pts)`。
- **止盈处理**：
  - 第一笔仓位的止盈目标按入场与止损距离的百分比 (`Take Profit %`) 计算。
  - 趋势持仓没有固定止盈，只通过止损、移动止损或反向信号退出。
- **额外逻辑**：
  - 反向信号会在建新仓前立即平掉反向持仓。
  - 仅使用收盘完成的 K 线数据，未完成的蜡烛不会触发信号。
- **默认参数**：
  - `First Volume` = 0.1
  - `Second Volume` = 0.1
  - `Take Profit %` = 50
  - `Pivot Offset (pts)` = 10
  - `Use Break-even Move` = true
  - `Break-even Offset (pts)` = 80
  - `Break-even Threshold (pts)` = 10
  - `Trailing Stop (pts)` = 80
  - `Trailing Step (pts)` = 120
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `Base Candle` = 1 小时
  - `MA1 Candle` = 日线
  - `MA2 Candle` = 4 小时
  - `MA1 Period` = 20
  - `MA2 Period` = 20
  - `ZigZag Depth` = 12
  - `ZigZag Deviation (pts)` = 5
  - `ZigZag Backstep` = 3
- **过滤条件**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：Bollinger Bands、移动平均、ZigZag
  - 止损：有（枢轴止损、保本、移动止损）
  - 复杂度：高级
  - 周期：多周期（基础 1 小时，过滤日线与 4 小时）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

## 说明

- 策略需要同时订阅三个不同周期的蜡烛数据，用于信号过滤与仓位管理。
  - ZigZag 枢轴检测通过限制深度、偏差与最小间隔来模拟 MetaTrader 的实现。
- 两笔仓位的手数可以单独调整，用于平衡固定止盈腿与趋势持仓腿的规模。
