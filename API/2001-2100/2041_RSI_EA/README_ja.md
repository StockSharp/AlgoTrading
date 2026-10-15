# RSI EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はクラシックなRSIエキスパートアドバイザーをエミュレートします。相対力指数が事前定義されたレベルをクロスしたときに取引し、ストップロス、テイクプロフィット、オプションのトレーリングストップでリスクを管理します。

## 戦略ロジック
- 設定可能な`RsiPeriod`を使用してRSIを計算します。
- **ロングエントリー**: RSIが`BuyLevel`を上回り、ロングポジションが存在しない場合。
- **ショートエントリー**: RSIが`SellLevel`を下回り、ショートポジションが存在しない場合。
- `CloseBySignal`が有効な場合、反対方向のクロスで既存ポジションを決済します。
- ポジションは価格単位で測定された`StopLoss`、`TakeProfit`、`TrailingStop`で保護できます。
- `CandleType`で定義されたローソク足データで動作します。

## パラメーター
- `OpenBuy` – ロングエントリーを有効にする。
- `OpenSell` – ショートエントリーを有効にする。
- `CloseBySignal` – 反対のRSIシグナルで決済する。
- `StopLoss` – 価格単位での損失。
- `TakeProfit` – 価格単位での利益。
- `TrailingStop` – 価格単位でのトレーリング距離。
- `RsiPeriod` – RSI計算の長さ。
- `BuyLevel` – ロングシグナルのRSI閾値。
- `SellLevel` – ショートシグナルのRSI閾値。
- `CandleType` – サブスクライブするローソク足の時間軸またはタイプ。

デフォルトの取引量は戦略の`Volume`プロパティで制御されます。
