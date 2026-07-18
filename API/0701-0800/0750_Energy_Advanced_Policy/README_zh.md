# Energy Advanced Policy 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Energy Advanced Policy** 策略将政策情绪与基础技术过滤器结合。

- **多头入场**: EMA(21) 上穿 EMA(55)，RSI 低于超买水平，布林带未处于收缩。
- **平仓**: RSI 高于超买水平或 EMA 趋势反转。

## 参数
- `NewsSentiment` – 手动情绪。
- `EnableNewsFilter` – 启用新闻过滤。
- `EnablePolicyDetection` – 启用政策事件检测。
- `PolicyVolumeThreshold` – 成交量倍数。
- `PolicyPriceThreshold` – 价格变动阈值 (%).
- `RsiLength` – RSI 周期。
- `RsiOverbought` – RSI 超买水平。
- `FastLength` – 快 EMA 周期。
- `SlowLength` – 慢 EMA 周期。
- `BbLength` / `BbMult` – 布林带参数。

指标: RSI、EMA、Bollinger Bands。
