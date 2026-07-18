# Fibo Arc Momentum戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTraderのエキスパートアドバイザー「FiboArc」（フォルダ`MQL/24924`）のStockSharpポートです。元のEAは複数のモメンタムフィルターとFibonacciaアークブレイクアウトを組み合わせています。StockSharp実装は同じアイデアを高水準キャンドルAPIに適応させながら維持します：

* 2つの線形加重移動平均（`FastMaPeriod`、`SlowMaPeriod`）がトレンド方向を定義します。
* ニュートラルレベル100に対して測定されたモメンタムオシレーターが弱いセットアップをフィルタリングします。
* MACDヒストグラムがトレンドの強さを確認し、新鮮なクロスオーバーを検出します。
* 簡略化されたFibonacciアークは、`TrendAnchorLength`と`ArcAnchorLength`で選択された2つのアンカーローソク足の始値を使用して各バーで再構築されます。この動的レベルを通じたブレイクアウトがMetaTraderバージョンのオブジェクトベースのチェックを置き換えます。

この戦略はStockSharpでサポートされている任意のシンボル/時間軸ペアで機能します。すべての計算はEAの動作を反映し、先読みバイアスを避けるために完全に完成したローソク足で実行されます。

## インジケーターとデータフロー

ストラテジーは`CandleType`で設定された単一のローソク足ストリームにサブスクライブします。各新しい完成したローソク足は`SubscribeCandles(...).BindEx(...)`を介して以下のインジケーターに供給されます：

| インジケーター | 目的 | デフォルト設定 |
|-----------|---------|------------------|
| LinearWeightedMovingAverage（高速） | 短期トレンドとエントリータイミング | `FastMaPeriod = 6`、典型的な価格 |
| LinearWeightedMovingAverage（低速） | 上位レベルトレンドフィルター | `SlowMaPeriod = 85`、典型的な価格 |
| Momentum | 100からの距離は強い動きを確認するために使用 | `MomentumPeriod = 14` |
| MovingAverageConvergenceDivergenceSignal | トレンドを確認しクロスオーバーを検出 | `MacdFastPeriod = 12`、`MacdSlowPeriod = 26`、`MacdSignalPeriod = 9` |

インジケーター出力は`IIndicatorValue`インスタンスとして受信されます；最終値のみが処理されます。

## Fibonacciアークの再構築

MetaTraderは実際のアークオブジェクトを描画し、`ObjectGetValueByShift`でその値を読み取ります。StockSharpはチャートオブジェクトに依存しないため、アークは数値的に模倣されます：

1. ストラテジーは完成したローソク足のローリングリスト（`_history`）を保持します。
2. `TrendAnchorLength`がベースアンカーのインデックスを選択し、`ArcAnchorLength`が2番目のアンカーを選択します。
3. 現在のローソク足のアークレベルは、`FibonacciRatio`（デフォルト0.618）を使用してアンカーの始値間の線形補間として計算されます。
4. ブレイクアウト検出のために、前のローソク足の始値と前のアークレベル、および現在のローソク足の始値と新たに計算されたレベルを比較します。下からのクロス（`fibCrossUp`）または上からのクロス（`fibCrossDown`）が元のEAチェックを再現します。

## 取引ルール

### ロングエントリー

以下のすべての条件が満たされるとロングポジションが開かれます：

1. 前のバーが前のアークレベルより下で開き、現在のバーが新しいレベルより上で開く（`fibCrossUp`）。
2. 高速LWMAが低速LWMAより上にある（`bullishTrend`）。
3. モメンタムと100の絶対距離が少なくとも`MomentumThreshold`である。
4. MACDメインラインがシグナルラインより上にある、またはちょうど上にクロスした（`macdAboveSignal`または`macdCrossUp`）。
5. 現在のポジションサイズがゼロ以下（既存のロングエクスポージャーなし）。

ストラテジーはフラットからロングへの移行を確実にするために、`Volume`プラスオープンショートエクスポージャーの絶対値を購入します。

### ショートエントリー

ショートトレードはロングロジックを反映します：

1. `fibCrossDown`が下へのブレイクアウトを確認する。
2. 高速LWMAが低速LWMAより下にある。
3. モメンタム距離が`MomentumThreshold`を超える。
4. MACDがシグナルラインより下にあるか、下にクロスする。
5. 既存のロングエクスポージャーが残らない。

### エグジット

以下のいずれかが発生するとポジションはクローズされます：

* トレンドまたはMACDの条件がトレードに対して反転する。
* 反対のFibonacciブレイクアウトシグナルが現れる。
* アダプティブストップロスまたはテイクプロフィットレベルに触れる。

すべてのエグジットは、即時クローズとトレーリングロジックを使用したMetaTraderバージョンとの一貫性を保つためにマーケット注文で実行されます。

## リスク管理

元のEAはマネーベースのストップ、トレーリングロジック、ブレイクイーブン保護を提供していました。StockSharpストラテジーは透明なパラメーターで同じ機能を維持します：

* `StopLossDistance`と`TakeProfitDistance`は執行価格からの価格単位での固定距離を定義します。
* `EnableBreakEven`、`BreakEvenTrigger`、`BreakEvenOffset`がブレイクイーブンへの移動動作を制御します。
* `EnableTrailing`、`TrailingTrigger`、`TrailingDistance`がローソク足ベースのトレーリングストップを実装します。

## パラメーター

| 名前 | 説明 |
|------|-------------|
| `CandleType` | すべての計算に使用される時間軸（および集計タイプ）。 |
| `FastMaPeriod`、`SlowMaPeriod` | トレンドを定義するLWMA長。 |
| `MomentumPeriod`、`MomentumThreshold` | モメンタムフィルター設定。 |
| `MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod` | MACD設定。 |
| `TrendAnchorLength`、`ArcAnchorLength`、`FibonacciRatio` | Fibonacciアーク再構築コントロール。 |
| `StopLossDistance`、`TakeProfitDistance` | 初期ストップとターゲット距離（絶対価格単位）。 |
| `EnableBreakEven`、`BreakEvenTrigger`、`BreakEvenOffset` | ブレイクイーブンロジック。 |
| `EnableTrailing`、`TrailingTrigger`、`TrailingDistance` | トレーリングストップ設定。 |

## 使用方法

1. ストラテジーをセキュリティにアタッチし、希望のポジションサイズに応じて`Volume`を設定します。
2. オプションで、ターゲット市場に合わせて時間軸、移動平均の長さ、Fibonacci設定を調整します。
3. ストラテジーを起動します。すべての決定は完成したローソク足に依存します；インバーバー実行は不要です。
4. ホストが視覚化をサポートする場合、高速/低速LWMAとMACDパネルの組み込みチャートヘルパーを確認します。
