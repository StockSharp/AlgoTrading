# 相对参考品种的相对强度图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图用交易品种与参考品种的比值构建一个合成品种，衡量该比值偏离自身 60 周期简单移动平均线的幅度，并据此对交易品种下单。比值高于自身均线百分之一，说明交易品种强于参考品种，图表做多；低于均线百分之一，说明其走弱，图表做空。委托单始终发往交易品种，合成品种只参与决策。

![schema](schema.svg)

## 策略概览

- 品种指数（Security index）方块按表达式 `BTCUSDT@BNBFT/TONUSDT@BNBFT` 构建合成品种。它的 K 线是以另一个品种为单位表示的某一品种价格，因此序列上行意味着交易品种相对参考品种走强。
- 该合成品种的五分钟已完成 K 线送入转换器（Converter），后者读取收盘价；同一序列还送入只在形成后输出的 60 周期 SimpleMovingAverage，相当于五小时。
- 公式（Formula）方块用比值的收盘价除以其均线再减一，得到以小数表示的相对强度：`+0.01` 表示比值高于自身五小时均线百分之一，`-0.01` 表示低于百分之一。
- 交易品种自身的一路五分钟已完成 K 线驱动决策循环。变量（Variable）方块的输入只作存储，保存最新的相对强度，并在交易品种的 K 线上放出该值，因此每次比较、每张委托单带的都是交易 K 线的时间戳，而不是合成 K 线的时间戳。
- 两个比较（Comparison）方块把放出的数值分别与阈值以及阈值的相反数作比较，后者由公式 `0 - a` 得出，使一个对外暴露的数值对称地控制多空两侧。
- 当前持仓（Position）两次与零比较，得到 `Position <= 0` 和 `Position >= 0`。两个逻辑条件（Logical condition）方块把各自的强度信号与对应的持仓判断合并，因此已经持有的一侧不会再次入场。
- 在空仓状态下，设为开仓（Open position）的持仓修改（Position modify）方块以市价买入或卖出固定的 Order Volume。图中没有止损方块，也没有止盈方块。
- 与新信号方向相反的持仓，先由设为平仓（Close position）的持仓修改方块平掉，反手则留给下一根满足条件的 K 线。

## 入场与出场规则

- **做多入场**: 在交易品种的一根已完成 K 线上，当放出的相对强度大于 Strength Threshold 且持仓不是多头时，多头闸门触发。空仓时，开仓方块以市价买入 Order Volume。若持有空头，这根 K 线上的入场被拒绝，因为先执行的是平仓动作；多头会在下一根仍显示走强的 K 线上开出。
- **做空入场**: 在交易品种的一根已完成 K 线上，当放出的相对强度小于 Strength Threshold 的相反数且持仓不是空头时，空头闸门触发。空仓时，开仓方块以市价卖出 Order Volume。若持有多头，这根 K 线上的入场被拒绝，因为先执行的是平仓动作；空头会在下一根仍显示走弱的 K 线上开出。
- **离场**: 图中没有独立的离场规则，没有止损，也没有止盈。持仓只在出现反向信号时离场：走强平掉空头，走弱平掉多头。平仓动作按已持有的仓位确定数量，因此其后账户为空仓；若信号仍在，反向的一侧会在下一根 K 线上开出。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | 构建合成品种所用的表达式。第一个品种是分子，第二个是作为参考的分母，因此分子相对参考走强时序列上行。改动它，即可用另一个参考品种来衡量交易品种。 |
| Ratio Candles | 00:05:00 | 合成品种 K 线的时间周期。它必须与交易序列一致，因为放出的强度是每根交易 K 线一个数值。 |
| Traded Candles | 00:05:00 | 交易品种 K 线的时间周期。每次比较、每次入场和每次离场都在该序列的每根已完成 K 线上计算一次。 |
| Reference Average Length | 60 | 比值的简单移动平均线所用的 K 线根数。六十根五分钟 K 线衡量最近五小时的强度；均线越长，衡量的背离越慢、越少见，成交也越少。 |
| Strength Threshold | 0.01 | 以小数表示的比值必须偏离均线的幅度，达到后才开仓。`0.01` 即百分之一，多头入场取均线上方，空头入场取均线下方。调低它交易更频繁，调高它则要求更大的背离。 |
| Order Volume | 1 | 每次入场的固定数量。平仓动作忽略它，改为按已持有的仓位确定数量。 |

## 图表详情

- [品种指数（Security index）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) 方块保存构建合成品种所用的表达式，并接入一个 [K 线（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 方块的 Security 输入。另一个 K 线方块保留在策略品种上，提供交易序列。两者都设为只输出已完成 K 线，因此未完成的 K 线不会把委托单的时间定在它自己这根 K 线的开始处。
- [转换器（Converter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) 读取合成 K 线的收盘价，只在形成后输出的 [指标（Indicator）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 对同一序列做 60 根 K 线的平均。[公式（Formula）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `a / b - 1` 把这一对数值变成一个带符号的小数，另一个公式 `0 - a` 把阈值镜像到走弱的一侧。
- 合成 K 线由两路数据拼装而成，其完成时间晚于同一分钟的普通 K 线。因此 [变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 在输入端保存强度值，只在交易 K 线给出的触发下发出，这正是让委托单时间戳保持在交易品种时钟上的原因。阈值、零和下单量这几个常量由同一根 K 线触发，使每个 [比较（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 方块在一次计算内同时拿到两个操作数。
- 每根交易 K 线上都把 [持仓（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 与零比较，两个 [逻辑条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 方块把强度结果与持仓结果合并。只有 `true` 的结果才会到达交易方块；`false` 的比较在触发输入处被丢弃。
- 四个 [持仓修改（Position modify）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 方块都以市价委托单动作。两个入场方块使用开仓（Open position）条件，因此只在空仓时动作，在已持有一侧时不会重复。两个离场方块使用平仓（Close position）条件，其数量取自已持有的仓位；当账户已经空仓或已经处于所要求的方向时，它不做任何动作。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
