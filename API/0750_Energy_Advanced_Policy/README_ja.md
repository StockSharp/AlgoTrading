# エネルギー高度ポリシー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Energy Advanced Policy**戦略は、政策センチメントと基本的なテクニカルフィルターを組み合わせます。

- **ロング**: EMA(21)がEMA(55)を上回り、RSIが買われすぎレベルを下回り、ボリンジャーバンドがスクイーズでない。
- **エグジット**: RSIが買われすぎレベルを上抜け、またはEMAトレンドが逆転。

## パラメーター
- `NewsSentiment` – 手動センチメント。
- `EnableNewsFilter` – ポリシーセンチメントの上書きを有効化。
- `EnablePolicyDetection` – ポリシーイベント検出を許可。
- `PolicyVolumeThreshold` – 出来高スパイクの倍率。
- `PolicyPriceThreshold` – 価格変動閾値 (%)。
- `RsiLength` – RSI期間。
- `RsiOverbought` – RSI買われすぎレベル。
- `FastLength` – 短期EMA期間。
- `SlowLength` – 長期EMA期間。
- `BbLength` / `BbMult` – ボリンジャーバンド設定。

インジケーター: RSI, EMA, Bollinger Bands。
