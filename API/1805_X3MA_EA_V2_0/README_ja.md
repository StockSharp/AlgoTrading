# トリプル移動平均クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高速・中速・低速の3本の移動平均の関係に基づいてトレードします。MQLエキスパート**X3MA_EA_V2_0**を変換したものです。

## トレードロジック

* **エントリー**
  * *EnableEntryMediumSlowCross*が真の場合、中速移動平均が低速移動平均を上抜けるとロングポジションが開かれます。逆のクロスオーバーがショートエントリーをトリガーします。
  * このオプションが偽の場合、戦略は高速平均が中速平均をクロスするのを待ちますが、その間、両方が低速平均の同じ側にある必要があります。ロングポジションは`fast > medium > slow`、ショートポジションは`fast < medium < slow`が必要です。
* **エグジット**
  * *EnableExitFastSlowCross*が真の場合、高速と低速の平均が逆方向にクロスしたとき、オープンポジションが決済されます。

すべてのシグナルは確定したローソク足で評価されます。

## パラメーター

| 名前 | 説明 |
|------|------|
| `FastMaLength` | 高速移動平均の期間。 |
| `MediumMaLength` | 中速移動平均の期間。 |
| `SlowMaLength` | 低速移動平均の期間。 |
| `EnableEntryMediumSlowCross` | 中速/低速クロスオーバーでのエントリーを許可する。 |
| `EnableExitFastSlowCross` | 高速/低速クロスオーバーでポジションを決済する。 |
| `CandleType` | ローソク足の時間軸。 |

## 注意事項

この戦略は`SubscribeCandles`と`Bind`を使った高レベルAPIを使用しています。インジケーターの値は`GetValue`を使わずに`ProcessCandle`コールバックを通じてアクセスされます。保護ロジックは`OnStarted`内の`StartProtection()`で有効化されます。
