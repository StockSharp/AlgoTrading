# Batman ATR トレーリングストップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルの「Batman」エキスパートアドバイザーにインスパイアされたATRベースのトレーリングストップアプローチを実装しています。
**Average True Range (ATR)**インジケーターから導出された動的な支持線と抵抗線を追跡し、価格がこれらのレベルを超えたときに反応します。

## ロジック

1. 設定可能な期間でATRを計算します。
2. 支持線と抵抗線を決定します:
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. 現在のトレンドに応じて最も近い支持線または抵抗線を維持します。
4. 価格が抵抗線を上抜けると、**ロング**ポジションを開きます。
5. 価格が支持線を下抜けると、**ショート**ポジションを開きます。

価格には終値または典型値 `(high + low + close) / 3` を使用できます。

## パラメーター

| 名前 | 説明 |
|------|------|
| `ATR Period` | ATRインジケーターの期間。 |
| `ATR Factor` | ストップラインを構築するためにATR値に適用される乗数。 |
| `Use Typical Price` | 有効にすると、終値の代わりに `(High + Low + Close)/3` を使用します。 |
| `Candle Type` | 計算に使用するローソク足の種類。 |

## 注意事項

- 戦略は `SubscribeCandles` と `Bind` を使用した高レベルAPIを利用します。
- 開始時にポジションの安全性を確保するため `StartProtection()` が呼び出されます。
- 取引は完了したローソク足のみで実行されます。
