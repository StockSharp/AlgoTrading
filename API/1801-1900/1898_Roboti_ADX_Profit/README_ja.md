# Roboti ADX Profit戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オリジナルの **RobotiADXProfitwining.mq4** エキスパートアドバイザーをStockSharp APIに変換したものです。トレンド方向の判定にDirectional Movement Index（DMI）を使用します。

## 取引ロジック

- デフォルト期間14の`DirectionalIndex`インジケーターを使用します。
- デフォルトでは1時間足ローソク足を使用しますが、時間軸は変更可能です。
- `+DI`ラインが`-DI`ラインを上抜けし、ロングポジションが未保有の場合に**ロング**ポジションを建てます。
- `-DI`ラインが`+DI`ラインを上抜けし、ショートポジションが未保有の場合に**ショート**ポジションを建てます。
- ポジションは価格の一定割合で設定されるトレーリングストップで保護されます。

## パラメーター

| 名前 | 説明 | デフォルト |
| ---- | ----------- | ------- |
| `DmiPeriod` | DMI計算の期間。 | 14 |
| `CandleType` | 戦略で使用するローソク足の種類と時間軸。 | 1時間 |
| `TrailingStopPercent` | トレーリングストップのサイズ（パーセント）。 | 1% |

## 注意事項

この戦略はStockSharpの高水準バインディングAPIを使用し、インジケーターバッファへの直接アクセスを避けています。コード内のすべてのコメントは英語で記述されています。
