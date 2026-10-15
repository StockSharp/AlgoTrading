# 稳定后突破限价单策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在已完成的五分钟 BTCUSDT@BNBFT K 线上寻找从实体稳定在 ATR(14) 一半以下到实体扩张至该水平以上的转变。它按信号收盘价挂出一张限价单，给予订单之后三根 K 线完成，并将已成交反转的基础数量加倍。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟 K 线提供 Open 和 Close 价格，已形成的 ATR(14) 值衡量当前波动尺度。
- Body 按 `abs(Close - Open)` 计算，稳定边界为 `ATR * Stabilization Factor`。该系数的默认值为 `0.5`。
- 第一组已形成的 Body/ATR 只初始化所保存的前一组值，不产生信号。之后每组已形成的数据都会将前一个实体和当前实体分别与对应边界比较。
- 设置条件是 `Previous Body < Previous ATR * 0.5` 且 `Current Body > Current ATR * 0.5`。任一处等于边界都不符合条件。
- 共用的待处理订单锁只允许一张活动限价单。独立的买卖生命周期门在各自三根 K 线计时器完成前禁止该方向再次使用计时器。

## 入场与出场规则

- **多头入场**：当符合条件的扩张 K 线为阳线（`Close > Open`）、有符号状态为空仓或空头、没有待处理订单且买方生命周期门已就绪时，按当前 Close 提交买入限价单。
- **空头入场**：当符合条件的扩张 K 线为阴线（`Close < Open`）、有符号状态为空仓或多头、没有待处理订单且卖方生命周期门已就绪时，按当前 Close 提交卖出限价单。
- **订单数量**：数量为 `Base Volume * (1 + abs(state))`。空仓入场使用一个基础单位；从 `-1` 或 `1` 反转时使用两个基础单位。
- **出场**：没有基于价格的保护。只有相反方向的限价单成交才会改变敞口；未成交限价单在严格后续的三根已完成 K 线之后收到定向撤单请求。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Security | BTCUSDT@BNBFT | 已完成五分钟 K 线订阅使用的证券。Strategy Security 必须设置为同一值，因为订单、撤单和成交使用 Strategy Security 与 Strategy Portfolio。 |
| Candle Series | 00:05:00 | 用于 ATR、实体计算、信号、生命周期计数和图表的已完成五分钟 K 线。 |
| ATR Length | 14 | Average True Range 指标的平均周期。只有 ATR 形成后才开始决策。 |
| Stabilization Factor | 0.5 | 分别乘以前一个和当前 ATR 值以得到各自实体边界的系数。 |
| Lifetime N | 3 | 对未成交限价单请求撤单前允许的严格后续已完成 K 线数量。 |
| Base Volume | 1 | 空仓入场数量；动作公式在反转时将其加倍。 |

## 图表详情

- Security [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 只配置已完成的 [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 订阅。每根完成的 K 线提供 Open、Close 和传入 ATR 的完整 K 线；订单与成交块使用 Strategy Security 和 Strategy Portfolio，因此 Strategy Security 必须与 Security 一致。
- [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 块仅输出已形成的 ATR(14) 值。公式与锁存块在一次 K 线决策内对齐当前 Body、当前边界、前一个 Body 和前一个边界。
- 初始化锁抑制第一次已形成决策并保存该数据对。之后的决策先检查两个严格的边界关系，再推进前值锁存块。
- 当前 K 线先进入两个生命周期计数器，随后信号分支才运行。在该输入之后启动计数器，使其在第三根更晚完成的 K 线上释放，因此信号 K 线不会计入自身生命周期。
- 接受设置后，在触发 [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) 前，先关闭相应方向的计时门并设置共用待处理锁。待处理锁只有在订单报告最终状态时才清除；即使订单提前成交，该方向的计时器也要到三根 K 线释放后才能再次使用。
- 每个注册块保存自己的 Order 引用以便定向撤单。生命周期释放把匹配的已保存引用送入撤单，并且只重新打开该方向的计时门。
- MyTrade 事件根据实际成交设置有符号状态：卖出成交设为 `-1`，买入成交设为 `1`，锁存器以表示空仓的 `0` 启动。图表接收 K 线、ATR、Body、稳定边界、已提交订单和策略成交。

## 使用方法

将 `.json` 文件导入 Designer，把 Strategy Security 设置为 BTCUSDT@BNBFT，在五分钟历史数据上运行，并在为其他交易环境调整系数、生命周期或数量之前检查限价单成交与撤单。
