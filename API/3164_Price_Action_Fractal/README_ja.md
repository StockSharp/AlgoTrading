# Price Action Fractal戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderのエキスパートアドバイザー「PRICE_ACTION」のC#移植版です。WilliamsフラクタルをLWMA（線形加重移動平均）、Momentum、MACDフィルターと組み合わせ、選択した時間軸で価格行動によって確認されたブレイクアウトを取引します。

## アイデア

1. 完成したローソク足のみを分析し、設定した時間軸のバークローズで全決定を行います。
2. 5本のローソク足ウィンドウを使用して新しい強気または弱気のフラクタルを検出します。新しい下降フラクタルは潜在的なサポートを示し、上昇フラクタルは潜在的なレジスタンスを示します。
3. 2つの線形加重移動平均（LWMA）で方向バイアスを確認します。ロング取引は速いLWMAが遅いLWMAより上にある必要があり、ショートは反対です。
4. 上位時間軸でMomentumインジケーターの中立レベル100からの絶対偏差を確認してMomentumを検証します。
5. MACDフィルター（デフォルト12,26,9）を使用：強気セットアップはMACDがシグナルラインより上にある必要があり、弱気セットアップはMACDがシグナルラインより下にある必要があります。
6. すべてのフィルターが一致したら、ブレイクアウトの方向にエントリーし、固定ストップ、トレーリングストップ、オプションのブレークイーブンシフトでポジションを管理します。

## エントリールール

- **ロングエントリー**
  - 現在のローソク足に新しい下降フラクタルが形成される（5本バーパターン）。
  - Fast LWMA &gt; Slow LWMA。
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`。
  - MACDメインライン &gt; MACDシグナルライン。
  - ポジションサイズは戦略のボリュームに基づき、`MaxPositionUnits`で制限されます。

- **ショートエントリー**
  - 現在のローソク足に新しい上昇フラクタルが形成される。
  - Fast LWMA &lt; Slow LWMA。
  - `abs(Momentum - 100)` &ge; `MomentumThreshold`。
  - MACDメインライン &lt; MACDシグナルライン。

## エグジット条件

- 固定ストップロス（`StopLossPoints`）と固定テイクプロフィット（`TakeProfitPoints`）を価格ステップで表現。
- オプションのトレーリングストップ（`TrailingStopPoints`）：ポジションがトレーリング距離以上の利益を得ると、最も有利な価格に追従します。
- オプションのブレークイーブン保護：`BreakEvenTriggerPoints`に達した後、ストップが`EntryPrice ± BreakEvenOffsetPoints`に移動されます。
- エグジットは市場注文で実行され、すべての計算はストップヒットを検出するためにローソク足の高値/安値に基づきます。

## ポジション管理

- 戦略はシンボルごとに単一の集計ポジションを維持します。
- `Volume`はベース注文サイズを定義します。逆転時、戦略はまず反対側のエクスポージャーをクローズし、次に要求されたサイズで新しいポジションを開きます。
- `MaxPositionUnits`はオーバーサイジングを避けるために絶対ポジション値を制限します。

## パラメーター

- `CandleType` – すべてのインジケーターと決定に使用される時間軸（MQL変数`T`に相当）。
- `FastMaPeriod` / `SlowMaPeriod` – 加重移動平均の長さ（`FastMA`、`SlowMA`）。
- `MomentumPeriod` – Momentumルックバック長（MQLスクリプトでは14に固定）。
- `MomentumThreshold` – Momentumを確認するために必要な100からの最小絶対偏差（`Mom_Buy`/`Mom_Sell`）。
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – MACD設定（デフォルト12/26/9）。
- `StopLossPoints`, `TakeProfitPoints` – 保護注文の価格ステップ距離（`Stop_Loss`、`Take_Profit`）。
- `TrailingStopPoints` – トレーリングストップ距離（`TrailingStop`）。
- `BreakEvenTriggerPoints`, `BreakEvenOffsetPoints` – ブレークイーブントリガーとオフセット（`WHENTOMOVETOBE`、`PIPSTOMOVESL`）。
- `FractalLifetime` – 検出されたフラクタルが有効な状態を維持するローソク足の数（`CandlesToRetrace`）。
- `MaxPositionUnits` – 最大絶対ポジションサイズ（ロット単位の`Max_Trades`制約）。
- `EnableTrailing`, `EnableBreakEven`, `UseStopLoss`, `UseTakeProfit` – 各エグジットメカニズムのスイッチ。

## 元のEAとの違い

- マネーベースのテイクプロフィット、エクイティストップ、メール/通知アラートなどのポートフォリオ全体の機能は実装されていません。
- MetaTraderのロット最適化ルーティンは簡略化されており、戦略はStockSharpのボリューム正規化を使用します。
- StockSharpのリスク管理方法が異なるため、保護注文は保留注文の変更ではなく市場エグジットで実行されます。

## ファイル

- `CS/PriceActionFractalStrategy.cs` – C#での戦略実装。
- Pythonバージョンはまだ提供されていません。
