# Jupiter M 策略
[English](README.md) | [Русский](README_ru.md)

基于 MetaTrader 专家 “Jupiter M. 4.1.1” 的网格策略。
算法使用可配置的步长构建订单篮，并在开启新层时调整
每个订单的止盈和手数。

## 细节

- **入场条件**：
  - 多头：价格下跌达到步长并且（可选）CCI < -100
  - 空头：价格上涨达到步长并且（可选）CCI > 100
- **方向**：双向
- **出场条件**：订单篮达到计算的止盈
- **止损**：在指定阶数后移动到盈亏平衡
- **默认值**：
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = 5 分钟K线
- **过滤器**：
  - 分类：网格，均值回归
  - 方向：双向
  - 指标：CCI（可选）
  - 止损：盈亏平衡
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：高

## 参数

- `TakeProfit` – 订单篮的价格单位止盈目标。
- `UseAverageTakeProfit` – 基于所有订单的平均价计算止盈。
- `DynamicTakeProfit` – 在 `TpDynamicStep` 之后使用 `TpDecreaseFactor` 递减止盈，最低不低于 `MinTakeProfit`。
- `BreakevenClose` / `BreakevenStep` – 在指定阶数后把目标移动到盈亏平衡。
- `FirstStep` – 网格之间的初始距离。
- `DynamicStep`、`StepIncreaseStep`、`StepIncreaseFactor` – 为每个新增订单增加步长。
- `MaxStepsBuy` / `MaxStepsSell` – 每个方向的最大订单数。
- `FirstVolume`、`VolumeMultiplier`、`MultiplyUseStep` – 控制网格中的手数增长。
- `CciFilter` / `CciPeriod` – 第一个订单的可选 CCI 过滤。
- `AllowBuy` / `AllowSell` – 启用交易方向。
- `CandleType` – 用于计算的K线周期。

该策略通过在价格偏离时分批建仓，并在动态止盈水平处平掉整篮仓位，
以此捕捉价格向均值回归的机会。
