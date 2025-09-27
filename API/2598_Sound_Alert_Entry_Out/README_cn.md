# Sound Alert Entry Out
[English](README.md) | [Русский](README_ru.md)

该辅助策略复刻了原始的 MetaTrader 专家顾问：当仓位被平掉时播放提示音。它监控所有会减少或反向当前仓位的成交，在检测到离场后播放所选终端音效，并且在启用通知时记录一条包含详细数据的消息。

## 详情

- **入场条件**：无。策略不会发送委托，可与其它策略或人工交易同时运行。
- **多空方向**：同时支持多头和空头，因为它对任何平仓成交都会做出反应。
- **离场条件**：成交方向与之前仓位方向相反时触发提醒流程。
- **止损**：无。风控由产生成交的策略或交易者负责。
- **默认值**：
  - `Sound` = NotificationSounds.Alert2
  - `NotificationEnabled` = false
- **提醒**：
  - 当检测到平仓时会播放所选 `.wav` 文件。
  - 当 `NotificationEnabled` 为 true 时，策略还会记录一条包含成交编号、方向、数量、标的符号以及成交处理后收益差额的消息。
  - 支持的音效：`alert`、`alert2`、`connect`、`disconnect`、`email`、`expert`、`news`、`ok`、`request`、`stops`、`tick`、`timeout`、`wait`。
- **使用提示**：
  - 可附加到任何会产生成交的账户/标的组合，逻辑基于账户成交而非指标。
  - 通知中的收益根据策略在成交前后的 PnL 差值计算。
  - 策略不需要订阅行情数据，无需绑定蜡烛或指标即可运行。

