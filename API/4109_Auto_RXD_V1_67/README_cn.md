# Auto RXD V1.67 策略（C#）

本文件夹包含 MetaTrader4 智能交易系统 **Auto_RXD_V1.67** 的 StockSharp C# 版本。原始 EA 使用三层线性加权均线感知器，并辅以多种经典指标过滤。本移植保留自适应感知器结构、ATR 风险模板以及主要过滤器，使策略在 StockSharp 高级 API 中重现原策略的逻辑。

## 核心思路

* 三个基于线性加权均线（LWMA）的“神经”感知器输出方向：
  * **Supervisor 感知器** 在 `Mode = AiFilter` 时为多空信号做总控。
  * **Long 感知器** 在 `Mode = AiLong` 时评估多头动能。
  * **Short 感知器** 在 `Mode = AiShort` 时评估空头动能。
* 可选的 **指标管理器** 使用 ADX、MACD（可要求金叉）、OsMA、抛物线 SAR、RSI、CCI、Awesome Oscillator 与 Accelerator Oscillator 过滤信号。
* 止盈止损既可设为固定点差，也可通过 ATR 倍数自适应计算。

## 交易模式

| 模式 | 说明 |
|------|------|
| `Indicator` | 仅依靠指标管理器给出方向。 |
| `Grid` | 在移植版本中关闭，仅保留枚举值。 |
| `AiShort` | 由空头感知器生成入场。 |
| `AiLong` | 由多头感知器生成入场。 |
| `AiFilter` | Supervisor 感知器需与多头/空头感知器同向才入场。 |

## 主要参数

* **General**：K 线类型、下单手数、交易时间窗口、是否允许新订单。
* **Risk**：是否使用 ATR、ATR 周期、长短方向的固定止盈止损点数。
* **Perceptrons**：Supervisor/Long/Short 三个感知器的 LWMA 周期、位移、四个权重与阈值。
* **Filters**：ADX、MACD、OsMA、SAR、RSI、CCI、AO、AC 的开关与细节参数。

## 交易流程

1. 订阅所选周期的 K 线。
2. 绑定 LWMA、ATR、MACD、ADX、SAR、RSI、CCI、AO、AC 指标。
3. 当全部指标形成后，根据模式计算感知器得分与过滤条件。
4. 信号成立时先平掉反向仓位，再按设定手数市价开仓，同时通过 `SetStopLoss` 与 `SetTakeProfit` 设置保护单。
5. 如启用订单管理器，当 SAR 反转时即时平仓。

## 使用说明

1. 在 StockSharp Designer 中加载解决方案。
2. 将 **AutoRxdV167Strategy** 拖入方案，绑定交易品种与时间框架。
3. 根据需要调整感知器权重、过滤器以及风险参数。
4. 运行回测或实盘，图表区域会显示 LWMA、MACD、ADX 与成交记录。

## 文件结构

* `CS/AutoRxdV167Strategy.cs` – 策略源码。
* `README.md` – 英文说明。
* `README_cn.md` – 中文说明。
* `README_ru.md` – 俄文说明。
