# ダイバージェンス MACD Stochastic 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader 5 エキスパート アドバイザー **「Divergence EA pip sl tp」** を StockSharp フレームワークで再作成します。このアルゴリズムは、価格変動と MACD ヒストグラムの間の古典的な乖離を検索し、反転取引を開始する前に買われすぎ/売られすぎの Stochastic オシレーター フィルターでシグナルを検証します。

## 取引ロジック

1. `CandleType` パラメータで選択されたプライマリ タイムフレーム ローソク足をサブスクライブします。
2. 完成したすべてのローソクの MACD ヒストグラム (`MACD line - Signal line`) と Stochastic %K/%D 値を計算します。
3. 価格とヒストグラム値の両方の最新の 2 つのスイング高値と安値を追跡します。
4. **弱気ダイバージェンス**: MACD ヒストグラムのピークの低下と `StochasticUpperLevel` の Stochastic %K を伴う新たな高値により、ショート ポジションがトリガーされるか、既存のロングが反転します。
5. **強気の発散**: MACD ヒストグラムの谷が高く、%K が `StochasticLowerLevel` を下回る新たな安値がオープンまたはリバースしてロングポジションになります。
6. オプションの保護 `TakeProfitSteps` と `StopLossSteps` は、StockSharp ステップ ユニットに変換され、戦略開始時に 1 回有効になります。

## 実装メモ

- `MovingAverageConvergenceDivergenceSignal` および `StochasticOscillator` インジケーターにバインドされた単一のローソク足サブスクリプションを使用して、StockSharp の高レベルの API で構築されています。
- 変換ガイドラインに従い、インジケーター `GetValue` ヘルパーを呼び出さずに発散状態を維持します。
- チャート統合では、チャート領域が使用可能な場合、価格ローソク足、MACD、および Stochastic 線が表示されます。
- 位置は、絶対的な現在位置のサイズをベース `Volume` に加算することによって反転され、発散が確認された後に即座に方向が変更されることが保証されます。

## パラメーター

| パラメータ | 説明 | デフォルト |
|-----------|-------------|---------|
| `CandleType` | 発散の計算に使用される時間枠。 | 1時間キャンドル |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | 元の EA 入力を複製する MACD EMA の長さ。 | 12/26/9 |
| `MacdDivergenceThreshold` | 発散を確認するために必要な、連続するスイング間のヒストグラムの差の最小値。 | 0.0005 |
| `StochasticLength` | Stochastic オシレーターの高速 %K 周期。 | 50 |
| `StochasticSlowK`, `StochasticSlowD` | EA 構成をミラーリングする追加の %K/%D スムージング長。 | 9/9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | 弱気/強気の設定を検証する買われすぎと売られすぎのフィルター。 | 80 / 20 |
| `TakeProfitSteps`, `StopLossSteps` | 価格ステップで表されるオプションの保護距離 (0 はレベルを無効にします)。 | 50 |

## 使用法

1. 選択した時間枠をサポートする証券を使用して、戦略を StockSharp コネクタに接続します。
2. 基本の `Volume` プロパティを通じてポジション サイズを設定し、必要に応じてインジケーター設定を調整します。
3. 戦略を開始します。発散条件と Stochastic 条件が満たされるたびに注文が自動的に生成されます。
