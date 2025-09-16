# Parabolic SAR Limit
[English](README.md) | [Русский](README_ru.md)

Parabolic SAR Limit 是 MT4 指标机器人 **ytg_Parabolic_exp.mq4** 的 StockSharp 版本。策略始终将买入和卖出限价单锁定在 Parabolic SAR 的数值附近，让价格“拉”单入场；成交后，再根据 K 线的最高价和最低价模拟原脚本中的止损/止盈退出流程。

## 策略逻辑

1. 订阅可配置的蜡烛序列（默认 4 小时）并计算 Parabolic SAR，参数 `step` 与 `maximum` 与 MT4 输入保持一致。
2. 每根收盘蜡烛都会执行以下检查：
   - 当 SAR 点位于蜡烛最低价之下，同时最优买价高于该 SAR 价格 `MinOrderDistancePoints` 个点时，在 SAR 位置放置或更新买入限价单。
   - 当 SAR 点位于蜡烛最高价之上，同时最优卖价低于该 SAR 价格 `MinOrderDistancePoints` 个点时，在 SAR 位置放置或更新卖出限价单。
   - 每个方向只保持一张挂单。如果 SAR 移动，旧挂单会被撤销并重新提交到最新价位。
3. 限价单成交后，将止损与止盈点数换算为绝对价格（使用品种的 `PriceStep`），并记录为虚拟的保护价位。
4. 后续每根蜡烛都会检查一次：若最高价或最低价触及保护价位，则调用 `ClosePosition()` 平仓，并清空保护状态。

## 参数说明

- **CandleType** – 信号蜡烛的时间框架。默认 4 小时，对应 MT4 中的 `timeframe` 参数。
- **SarStep** – Parabolic SAR 的加速度系数（即 MT4 的 `step`）。控制 SAR 追随价格的速度。
- **SarMaximum** – 最大加速度（即 MT4 的 `maximum`）。限制 SAR 的最高速度。
- **StopLossPoints** – 入场价到止损价的点数距离。设置为 `0` 时禁用止损。
- **TakeProfitPoints** – 入场价到止盈价的点数距离。设置为 `0` 时禁用止盈。
- **MinOrderDistancePoints** – 对应 MT4 的 `MODE_STOPLEVEL`。只有当市场价格距离 SAR 足够远时才会下单。
- **OrderVolume** – 每张限价单的手数/数量，请与品种的 `VolumeStep` 保持一致。

所有点数都会按照 `PriceStep` 自动转换为价格单位，因此可适配不同报价精度的品种。

## 交易特性

- 策略可同时在多空两个方向挂单；当 SAR 翻转时，买单与卖单可以同时存在。
- 挂单始终与最新的 SAR 值保持一致，旧价位会被取消并重新登记。
- 止损/止盈采用虚拟方式，通过蜡烛高低价来判断是否触发，因为高层 API 无法直接给挂单附带 SL/TP。
- 优先使用 Level 1 的最优买卖报价；若无该数据，会退化到使用蜡烛收盘价作为当前价格的近似值。

## 移植注意事项

- `MinOrderDistancePoints` 默认为 0，如交易所或券商要求最小挂单距离，可自行设置。
- 当仓位平仓或挂单取消后，策略会自动重置保护价位，防止旧数据影响后续交易。
- C# 代码中加入了详细的英文注释，解释如何使用高层 API 绑定指标与管理订单生命周期。

## 使用建议

- 尽量提供 Level 1 行情，使距离判断更加准确；如果行情缺失，请确认蜡烛收盘价是否能够代表当前市场价。
- 检查交易品种的 `PriceStep` 与 `VolumeStep`，确保点数、数量经过换算后仍符合交易所的最小变动要求。
- 若希望更细粒度地检测止损，可选择更短的蜡烛周期，以便更频繁地评估保护价位。
