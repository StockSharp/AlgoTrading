# Bollinger Heikin Ashi Entry

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

在 Heikin Ashi 蜡烛上计算布林带反转，实际平掉一半持仓获利，再跟踪剩余仓位。

## 交易规则

HA 收盘为 `(Open+High+Low+Close)/4`；首根 HA 开盘为 `(Open+Close)/2`，之后为 `(前 HA 开盘+前 HA 收盘)/2`。HA 最高/最低含 HA 开收盘与原始高低价。布林带从第一根存档蜡烛开始使用 HA 收盘价，而非原始收盘价。十字星不属于阴或阳。触及定义为 `HA low <= lower` / `HA high >= upper`。

前面至少两根连续阴线 HA 蜡烛必须各自触及其下轨；随后阳线 HA 蜡烛收盘严格高于自身下轨时做多。颜色、上轨触及和收盘方向反向时做空。

仅空仓时以 `Volume` 入场。初始止损为前一根已完成原始蜡烛的低点（多）或高点（空），发单前固定。`R` 是加权实际入场价到止损的正距离。达到 `1R` 后发市价单平一半实际仓位，按 `VolumeStep` 向下取整。若任一半小于 `MinVolume`，则在目标处全平。若入场成交越过初始止损，在下一个可执行报价平仓。

只有目标单实际成交才启用跟踪；剩余仓位止损沿最新已完成原始蜡烛的低点/高点移动，绝不放宽。多头 bid 或空头 ask 在蜡烛之间触发全量止损，止损优先于目标。未完成订单阻止重复发单。较早的蜡烛极值不会触发后来创建的价位。没有固定百分比目标或隐藏等待期。

仅在指标充分预热后使用已完成的蜡烛生成入场信号。持仓持续管理至退出；未完成的市价订单会阻止重复提交。

布林带采用滚动收盘价窗口的总体标准差，包含当前已完成蜡烛。

## 参数

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerDeviation` | `2` |
| `CandleType` | `5m` |
