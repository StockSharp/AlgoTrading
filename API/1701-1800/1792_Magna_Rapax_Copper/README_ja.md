# Magna Rapax Copper戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルのMQLエキスパートの「レインボー」移動平均システムを再現します。
11本の指数移動平均線をMADCとADXフィルターと組み合わせて使用します。

## 仕組み

- 終値に対してEMA(2)、EMA(3)、EMA(5)、EMA(8)、EMA(13)、EMA(21)、EMA(34)、EMA(55)、EMA(89)、EMA(144)、EMA(233)を計算します。
- MACD（速い、遅い、シグナル）を計算し、シグナルラインを使用します。
- トレンド強度を測定するためにADXを計算します。
- **買い**条件：
  - MACDシグナルラインがゼロより上にある。
  - すべてのEMAが厳密に上昇している（各速いEMAが遅いEMAより上にある）。
  - ADX値がしきい値を超えている。
- **売り**条件：
  - MACDシグナルラインがゼロより下にある。
  - すべてのEMAが厳密に下降している。
  - ADX値がしきい値を超えている。

反対のシグナルが現れるとポジションが反転します。

## パラメーター

| 名前 | 説明 |
| --- | --- |
| `FastMacd` | MACDの速いEMAの期間。 |
| `SlowMacd` | MACDの遅いEMAの期間。 |
| `SignalPeriod` | MACDのシグナルライン期間。 |
| `AdxPeriod` | ADXインジケーターの期間。 |
| `AdxThreshold` | 取引に必要な最小ADX値。 |
| `CandleType` | 計算に使用するローソク足の時間軸。 |

## 注意事項

- 戦略は`BuyMarket`と`SellMarket`を通じて成行注文を使用します。
- 一度に保持するポジションは1つだけで、反対のシグナルでポジションが反転します。
- これはオリジナルのMQL戦略の直接変換であり、オプションのマーチンゲールロジックは含みません。
