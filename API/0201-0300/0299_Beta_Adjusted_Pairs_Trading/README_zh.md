# Beta Adjusted Pairs Trading

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

两个工具的 beta 中性均值回归。必须显式提供不同的 `Asset2` 和 `Asset2Portfolio`（可与主账户相同），没有隐式 TON 或账户。两个 beta 必须为正。只使用 `OpenTime` 完全相同的已完成蜡烛；缺失、陈旧或重复数据不得混合不同时间。

## 交易规则

`h=BetaAsset1/BetaAsset2`；`B1,B2` 是首对正收盘价。价差 `s=ln(P1/B1)-h*ln(P2/B2)`。使用最近 `LookbackPeriod` 对价差（含当前）的均值和总体标准差计算 `z=(s-mean)/deviation`。标准差为零则不入场。多头条件 `z[p]<=-EntryThreshold && -EntryThreshold<z<0`；空头条件 `z[p]>=EntryThreshold && 0<z<EntryThreshold`。仅停留在阈值之外不触发。

多头买入主工具 `Volume` 并卖出第二工具 `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2`；空头反向。采用匹配信号蜡烛价格并验证第二工具数量限制。持仓中不改变该数量。入场要求两腿均空仓且无未完成订单；连续入场索引之差必须严格大于 `CooldownBars`。

每对已完成蜡烛按实际加权成交价 `F1,F2` 和带符号成交数量 `q1,q2` 计算 `PnL=q1*(P1-F1)+q2*(P2-F2)`，`G=abs(q1)*F1+abs(q2)*F2`。若 `PnL<=-G*StopLoss/100` 则平掉实际两腿；否则多头在 `z>=-ExitThreshold`、空头在 `z<=ExitThreshold` 退出。亏损优先，入场等待期不延迟退出。这是手续费前的收盘价组合亏损限制，不是盘中交易所止损。

两腿是独立市价单，并非原子交易。未完成订单阻止新决策。订单结束后若有缺失、拒绝或不完整的腿，在下一对匹配蜡烛平掉实际残余仓位；失败的平仓在后续匹配对重试。BTC/TON 存档回放只验证机制，不证明收益或稳定经济关系。

仅在指标充分预热后使用已完成的蜡烛生成入场信号。持仓持续管理至退出；未完成的市价订单会阻止重复提交。

## 参数

| Parameter | Default |
|---|---|
| `Asset2` | `required` |
| `Asset2Portfolio` | `required` |
| `BetaAsset1` | `1` |
| `BetaAsset2` | `1` |
| `LookbackPeriod` | `20` |
| `EntryThreshold` | `2` |
| `ExitThreshold` | `0.15` |
| `StopLoss` | `2 (%)` |
| `CooldownBars` | `120` |
| `CandleType` | `5m` |
