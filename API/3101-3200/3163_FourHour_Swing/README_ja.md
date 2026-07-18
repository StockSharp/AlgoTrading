# Four Hour Swing戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Four Hour Swing戦略**は、MetaTraderの「4Hスイング」エキスパートアドバイザーをStockSharpの高レベルAPIに移植したものです。元のシステムは、高い時間軸から取得したトレンドフォローとオシレーター確認を組み合わせています。このC#バージョンは3つの時間軸（エントリー、確認、マクロフィルター）をサブスクライブし、StockSharpコンポーネントでインジケーターのスタックを再現します。

## トレードロジック
- メインのトレンドフィルターは、エントリーローソク足の典型価格で計算された3つの指数移動平均を使用します。ロングのセットアップには `Fast EMA > Medium EMA > Slow EMA` が必要で、ショートはその逆です。
- Stochasticオシレーターの値は、高い確認時間軸で評価されます。%Kラインは、ロングでは%Dより上、ショートでは下に留まる必要があります。
- Momentumは同じ確認ローソク足からサンプリングされ、100前後のMetaTraderスタイルの比率に変換されます。過去3つのMomentum読み値のうち少なくとも1つが設定された閾値より遠い場合のみ取引が許可されます。
- 月次（またはユーザー定義の）MACDの値がマクロ方向フィルターを提供します。買いはMACDラインがシグナルより上にある必要があり、売りは反対の関係を確認します。

すべての確認が一致し、口座がフラットまたは反対方向にポジションを持っている場合（その場合、市場注文がクローズして反転する）、次のベースローソク足でポジションが開かれます。

## リスク管理
- 固定のストップロスとテイクプロフィットの距離（pipsで表現）がエントリー直後に適用されます。
- オプションのトレーリングストップがエントリー後に達した極値価格に従います。
- Break-even保護は、設定されたトリガー距離に達すると、ストップをエントリー価格にオフセットを加えた位置に移動できます。
- オプションのMACD出口は、マクロフィルターが反転したときにオープンな取引を閉じます。

## パラメーター
| 名前 | 説明 | デフォルト値 |
| --- | --- | --- |
| `TradeVolume` | デフォルトの市場注文ボリューム。 | `0.01` |
| `CandleType` | エントリーローソク足タイプ（例：4時間ローソク足）。 | `4H` |
| `SignalCandleType` | StochasticとMomentumの確認ローソク足タイプ。 | `7D`（週次） |
| `MacdCandleType` | マクロフィルターのローソク足タイプ。 | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | 典型価格で計算されるEMAの長さ。 | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | Stochasticオシレーターの設定。 | `13`, `5`, `5` |
| `MomentumPeriod` | Momentumインジケーターが使用するルックバック。 | `14` |
| `MomentumThreshold` | Momentumを検証するために必要な100からの最小距離。 | `0.3` |
| `StopLossPips`, `TakeProfitPips` | pipsでの保護注文。 | `20`, `50` |
| `TrailingStopPips` | pipsでのトレーリングストップ距離。無効にするには0に設定。 | `40` |
| `UseBreakEven` | Break-even保護を有効にします。 | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-evenの移動のためのトリガーとオフセット。 | `30`, `30` |
| `UseMacdExit` | マクロMACDが反転したときにポジションを閉じます。 | `false` |

## 備考
- 実装をコンパクトに保つために、元のエキスパートからの資金管理機能（エクイティストップ、通貨ターゲット）は意図的に省略されています。
- 戦略は完成したローソク足のみを処理し、MetaTraderのバー単位の評価と一致します。
- デフォルトの時間軸は一般的な4時間セットアップ（週次確認と月次フィルター）を再現しますが、すべての `DataType` パラメーターは代替期間で実行するように再設定できます。
