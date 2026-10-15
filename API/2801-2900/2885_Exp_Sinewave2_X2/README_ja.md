# Exp Sinewave2 X2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Exp Sinewave2 X2はJohn EhlersのSinewave分析に着想を得たマルチタイムフレームのトレンドフォロー戦略です。高い時間軸フィルターが支配的な方向を定義し、低い時間軸が正確なエントリーとエグジットのトリガーを提供します。すべての計算は再構築されたSinewave2インジケーターを使用し、これは内部で適応CyclePeriodモジュールに依存します。

## インジケーター
- **高い時間軸Sinewave2（リードラインvsサインライン）** – リードサインが主サインコンポーネントを交差することで強気または弱気バイアスを検出します。
- **低い時間軸Sinewave2** – 高い時間軸方向と一致したトレードをトリガーするために最新のクロスオーバーイベントを監視します。

## 取引ロジック
1. **トレンドフィルター**
   - 高い時間軸でSinewave2を計算します。
   - `SignalBarHigh`バー前のリードとメインラインを評価します。
   - `Lead > Sine`の場合は強気トレンド、`Lead < Sine`の場合は弱気トレンド、それ以外はニュートラルです。
2. **エントリーシグナル**
   - 低い時間軸で完了したロウソク足を待ちます。
   - `SignalBarLow`（現在）と`SignalBarLow + 1`（前）で定義されたオフセットでリードとサインの値を取得します。
   - ロングエントリー：前のクロスオーバーが下向き（以前は`Lead > Sine`、現在は`Lead <= Sine`）で、高い時間軸のトレンドが強気かつ`EnableBuyOpen`が有効。
   - ショートエントリー：前のクロスオーバーが上向き（以前は`Lead < Sine`、現在は`Lead >= Sine`）で、高い時間軸のトレンドが弱気かつ`EnableSellOpen`が有効。
3. **エグジットルール**
   - 低い時間軸の出口ブール値`EnableBuyCloseLower`と`EnableSellCloseLower`は反対のクロスオーバーでポジションを決済します。
   - 高い時間軸の出口ブール値`EnableBuyCloseTrend`と`EnableSellCloseTrend`は主要トレンドがオープン方向に反転するたびにポジションを即座に決済します。
   - 保護ストップロスとテイクプロフィットはイントラバーの高値/安値と価格ステップで表された`StopLossPoints` / `TakeProfitPoints`距離を使用して各ロウソク足で評価されます。
4. **リスク管理**
   - ポジションリバーサルは新しい注文を`Volume + |Position|`としてサイジングし、新しいものを確立する前に既存のポジションをフラット化します。
   - 各エントリー後、`SetRiskLevels`は`Security.PriceStep`を使用して絶対的なストップ/ターゲット価格を再計算します（利用不可の場合はフォールバック1）。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `AlphaHigh` | 高い時間軸Sinewave2フィルターのアルファ係数。 |
| `AlphaLow` | 低い時間軸Sinewave2トリガーのアルファ係数。 |
| `SignalBarHigh` | トレンド状態を読み取るために高い時間軸で遡るバー数。 |
| `SignalBarLow` | クロスオーバー状態を読み取るために低い時間軸で遡るバー数。 |
| `EnableBuyOpen` / `EnableSellOpen` | 低い時間軸シグナルからのロング/ショートエントリーを許可。 |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | 高い時間軸がポジションに反転したときの強制出口。 |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | 低い時間軸の反対のクロスオーバーでポジションを決済。 |
| `StopLossPoints` | インジケーターの価格ステップで表されたストップロス距離。 |
| `TakeProfitPoints` | インジケーターの価格ステップで表されたテイクプロフィット距離。 |
| `HigherCandleType` / `LowerCandleType` | フィルターとトリガーストリームのロウソク足データタイプ（時間軸）。 |

## 注意事項
- 戦略は完了したロウソク足のみを処理し、部分的な更新は無視します。
- 適応Sinewave2実装はMQLバージョンに忠実であるために元のCyclePeriodアルゴリズムを使用します。
- 高い時間軸と低い時間軸のロウソク足タイプが同一の場合、冗長なデータリクエストを避けるために両方のインジケーターが1つのロウソク足サブスクリプションを共有します。
- デプロイ前にベース`Strategy`の`Volume`を調整してトレードサイズを制御します。
