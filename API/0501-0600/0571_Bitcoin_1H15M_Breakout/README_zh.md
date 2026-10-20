# Bitcoin 1H/15M Breakout

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

使用已完成的 15 分钟足交易前一完整小时区间的突破。低周期足聚合至 UTC 小时桶；只有从精确小时起点开始的四根连续季度足全部存在，该小时才有效。初始不完整小时或数据缺口不能生成区间。

对开盘位于 H 小时的低周期足，只使用已完成 H−1 小时的最高/最低价，不使用正在形成的当前小时。空仓时，收盘严格高于该最高价买入 `Volume`，严格低于最低价卖出 `Volume`。允许双向，已有仓位不因突破而加仓或反转；活动订单阻止下一单。

原生本地保护将 `StopLossBuffer` 作为距实际入场成交的绝对价差，默认 50。止盈距离为 `StopLossBuffer * RiskRewardRatio`，默认比率 2 对应距离 100。两者均市价全仓退出，之后小时区间变化不移动保护水平。`HigherTimeFrame` 必须是低周期的整数倍。

本地原生保护使用该品种的蜡烛和已订阅价格更新，距离以实际入场成交为基准。退出为市价订单，成交价受可用市场及价格步长影响。

## 参数

| 参数 | 默认值 |
|---|---|
| `CandleType` | `15m` |
| `HigherTimeFrame` | `1h` |
| `StopLossBuffer` | `50` |
| `RiskRewardRatio` | `2` |
