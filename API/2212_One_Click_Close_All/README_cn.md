# 一键平仓策略
[English](README.md) | [Русский](README_ru.md)

该工具策略在启动时关闭所有持仓，并可选地取消挂单。适合在一次操作中快速清空账户。

## 详情

- **用途**：关闭持仓并取消挂单
- **入场条件**：无 —— 启动即执行
- **多/空**：取决于当前持仓
- **出场条件**：不适用（执行后即停止）
- **止损**：无
- **默认值**：
  - `RunOnCurrentSecurity` = true
  - `CloseOnlyManualTrades` = true
  - `DeletePendingOrders` = false
  - `MaxSlippage` = 5
- **筛选**：
  - 分类：工具
  - 方向：双向
  - 指标：无
  - 止损：无
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：可变
