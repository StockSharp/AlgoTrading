# EA MA Email Alert Strategy
[English](README.md) | [Русский](README_ru.md)

## 概述
- 完整复刻 MetaTrader 4 专家顾问“EA_MA_Email”，通过蜡烛开盘价计算的一组指数移动平均线（EMA）来进行监控。
- 当所选 EMA 组合发生向上或向下交叉时，生成与原始邮件完全一致的日志消息，实现邮件提醒的模拟。
- 该策略不会下单，可作为监控工具附加到任意品种，只用于获取交叉提示。

## 指标配置
- **EMA 20、EMA 50、EMA 100、EMA 200**
  - 所有 EMA 都使用蜡烛的开盘价作为输入，以保持与 MQL 版本一致。
  - `CandleType` 参数决定蜡烛的时间框架，也控制 EMA 的更新节奏。

## 信号逻辑
1. 策略订阅指定类型的蜡烛，并将开盘价传入四条 EMA。
2. 当所有 EMA 已经形成后，会保存每条启用组合的前一根与当前一根值。
3. 如果快线在上一根低于慢线，而当前收于其上，则触发看涨提醒。
4. 如果快线在上一根高于慢线，而当前收于其下，则触发看跌提醒。
5. 每条提醒都会按照 MT4 邮件的主题/正文格式写入日志，包含品种、交叉方向、周期名、收盘时间与价格。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SendEmailAlert` | `bool` | `true` | 是否将模拟邮件提醒写入策略日志。 |
| `Monitor20Over50` | `bool` | `false` | 监控 EMA 20 与 EMA 50 的交叉。 |
| `Monitor20Over100` | `bool` | `false` | 监控 EMA 20 与 EMA 100 的交叉。 |
| `Monitor20Over200` | `bool` | `false` | 监控 EMA 20 与 EMA 200 的交叉。 |
| `Monitor50Over100` | `bool` | `false` | 监控 EMA 50 与 EMA 100 的交叉。 |
| `Monitor50Over200` | `bool` | `true` | 监控 EMA 50 与 EMA 200 的交叉。 |
| `Monitor100Over200` | `bool` | `false` | 监控 EMA 100 与 EMA 200 的交叉。 |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 用于 EMA 计算与提醒的蜡烛类型。 |

## 提醒格式
- 主题示例：`EURUSD 50>200 PERIOD_H1`
- 正文示例：`Date and Time: 2024-05-08 13:00:00; Instrument: EURUSD; Close: 1.07543`
- 所有提醒通过 `AddInfoLog` 输出，可进一步对接任意日志或通知系统。

## 使用说明
- 提醒基于完整蜡烛生成，因此只有当蜡烛收盘后才会触发，盘中波动不会立即产生信号。
- 周期名转换支持 MetaTrader 常用的 M1、M5、M15、M30、H1、H4、D1、W1、MN1，并保持与原专家一致的文本。
- 若仅需继续计算 EMA 而暂时不想接收提醒，可关闭 `SendEmailAlert` 参数。
- 策略不会发送交易指令，在真实连接上运行也是安全的。
