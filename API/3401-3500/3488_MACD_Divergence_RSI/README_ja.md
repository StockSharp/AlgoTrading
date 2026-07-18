# MACD ダイバージェンス RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader エキスパート アドバイザー **「Macd diver rsi mt4」** を StockSharp のハイレベル API に移植します。
- MACD ダイバージェンス認識と組み合わせた RSI フィルターを使用して、単一のシンボルを時間反転にトレードします。
- 一度にオープンできる市場ポジションは 1 つだけです。この戦略は、新しいシグナルを発行する前にフラット状態になるのを待ちます。

## 信号ロジック
1. 選択した時間枠からの終了したすべてのローソク足は、戦略にバインドされた 4 つのインジケーターをフィードします。
   - 2 つの独立した `RelativeStrengthIndex` インスタンス (売られすぎフィルターと買われすぎフィルター用) が 1 バー分サンプリングしました。
   - 構成可能な高速/低速 EMA と信号長を備えた 2 つの `MovingAverageConvergenceDivergence` インジケーター。
2. **強気のセットアップ**
   - 前のバー RSI は、設定可能な売られすぎしきい値を下回っている必要があります。
   - 最新の MACD 値は、動的しきい値 (現在の商品の 3 ピップに相当) を下回る局所的なディップを形成する必要があります。
   - 過去のデータがスキャンされ、以前の MACD の下落とそれに関連する価格変動の安値が特定されます。乖離が確認された場合
MACD の谷が上昇する一方で価格が安値を切り下げる（通常のダイバージェンス）、または MACD の谷が下落する一方で価格が高くなる
低い (隠れた発散)、元の MQL ロジックと一致します。
   - 確認され、戦略にオープンポジションがない場合、方向固有のボリュームとリスク設定で市場買いが送信されます。
3. **弱気の設定**は、RSI の買われすぎフィルターと MACD のピークを備えた強気のルールを反映しています。発散は次によって検証されます
以前のスイング高値と現在のスイング高値を比較します。
4. エントリー直後、戦略は設定されたストップロスとテイクプロフィットの距離をピップから価格単位に変換します。
(元のポイント形式ルールを尊重して) `SetStopLoss` / `SetTakeProfit` を通じて適用します。

## パラメーター
- `LowerRsiPeriod`、`LowerRsiThreshold` – `inp1_Lo_RSIperiod` / `inp1_Ro_Value` にマッピングします。
- `BullishFastEma`、`BullishSlowEma`、`BullishSignalSma` – `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA` にマッピングします。
- `BullishVolume`、`BullishStopLossPips`、`BullishTakeProfitPips` – `inp3_VolumeSize`、`inp3_StopLossPips`、`inp3_TakeProfitPips` にマッピングします。
- `UpperRsiPeriod`、`UpperRsiThreshold` – `inp4_Lo_RSIperiod` / `inp4_Ro_Value` にマッピングします。
- `BearishFastEma`、`BearishSlowEma`、`BearishSignalSma` – `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA` にマッピングします。
- `BearishVolume`、`BearishStopLossPips`、`BearishTakeProfitPips` – `inp6_VolumeSize`、`inp6_StopLossPips`、`inp6_TakeProfitPips` にマッピングします。
- `CandleType` – すべての計算のタイムフレーム ソース。

## 実装メモ
- MACD 発散閾値は現在の商品ポイント サイズから導出され、3 ピップに等しく、デフォルトの 0.0003 に一致します。
MQL バージョンで使用されます。
- ローソク足、MACD、および価格履歴は、境界付きリスト (600 要素) に保存され、発散スキャン ウィンドウを再現します。
大きな配列を割り当てる。
- この戦略は、`SubscribeCandles(...).Bind(...)` を使用して単一パスですべてのインジケーターを更新し、プロセスのみが終了します
ローソク足は、元のバーごとに 1 回のブロック実行と同じです。
- ピップ距離は、`SetStopLoss` と `SetTakeProfit` を呼び出す前に絶対価格オフセットに変換され、
MQL ソースの先頭で宣言されたポイント形式ルール。
