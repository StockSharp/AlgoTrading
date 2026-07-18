# MACD あまりサンプル戦略ではありません
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
MACD Not So Sample 戦略は、MetaTrader エキスパート アドバイザー *MACD_Not_So_Sample* を変換したものです。オリジナルのロボット取引
EMA トレンド フィルターで確認された MACD クロスオーバーを使用し、大きなテイクプロフィット レベルと組み合わせた 4 時間の EURUSD チャート
トレーリングストップ。 StockSharp バージョンは同じ構造を維持します。MACD ヒストグラムは負であり、その信号を超える必要があります
ラインはロングエントリーを表し、シグナルの下を横切る正のヒストグラムはショートエントリーを生成します。トレンド EMA は、
ポジションがオープンされる前の方向。

すべての資金管理機能は StockSharp に実装されています。戦略は構成可能な利益獲得目標を設定し、利益を管理します。
価格が十分に上昇したらトレーリングストップ、MACDが十分な値で反対方向にクロスしたときに取引を終了します
強さ。ポートは StockSharp インジケーターと高レベルのローソク足サブスクリプションを使用するため、すべての計算は最終的な H4 で行われます。
キャンドルは、MetaTrader の動作を反映しています。

## 取引ロジック
1. `CandleType` で定義された時間枠 (デフォルトは 4 時間足のローソク足) をサブスクライブし、終了したローソク足のみを処理します。
2. `MovingAverageConvergenceDivergenceSignal` インジケーターに、構成された `FastPeriod`、`SlowPeriod`、および
`SignalPeriod`。このインジケーターは、MACD ラインとシグナル ラインの両方を提供します。
3. 長さ `TrendPeriod` の EMA トレンド フィルターを計算します。その傾きによって、ロングエントリーが許可されるかショートエントリーが許可されるかが決まります。
4. pip ベースのしきい値 (`MacdOpenLevelPips`、`MacdCloseLevelPips`、`TakeProfitPips`、`TrailingStopPips`) を絶対値に変換します
金融商品のピップサイズを使用した価格距離。
5. ポジションが存在しない場合:
   - MACD がゼロ未満、現在の値がシグナル値を上回っている、前の MACD がシグナル値を下回っていた場合、**ロング** ポジションをオープンします
前の信号、EMA は上昇しており、MACD の大きさは `MacdOpenLevelPips` を超えています。
   - MACD がゼロより上、現在の値がシグナル値より下、前の MACD が上だった場合、**ショート** ポジションをオープンします
前の信号、EMA は立ち下がり、MACD の大きさは `MacdOpenLevelPips` を超えています。
6. ロングポジションを保持している場合:
   - MACD がプラスになり、シグナルを下回り、その大きさが `MacdCloseLevelPips` を超えたときに取引を終了します。
   - 価格が設定されたテイクプロフィットに達した場合、またはトレーリングストップレベルを突破した場合は、早期に終了します。
7. ショートポジションを保持している場合:
   - MACD がマイナスに転じ、シグナルを上回り、その大きさが `MacdCloseLevelPips` を超えたときに取引を終了します。
   - 価格が利益確定ターゲットまたはトレーリングストップに達した場合は、早期に終了します。
8. トレーリングストップは、価格がしきい値を `TrailingStopPips` まで超えて、利益を確定した後にのみ有効になります。
その後のローソク足の極値に続きます。

## パラメーター
| 名前 | 種類 | デフォルト | 説明 |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | MACD 計算内で使用される高速な EMA の長さ。 |
| `SlowPeriod` | `int` | `166` | MACD 計算内で使用される遅い EMA の長さ。 |
| `SignalPeriod` | `int` | `11` | MACD 信号線の EMA の長さ。 |
| `TrendPeriod` | `int` | `8` | EMA トレンド フィルターの長さ。 |
| `MacdOpenLevelPips` | `decimal` | `1` | ポジションをオープンするために必要な最小値 MACD の大きさ (ピップ単位)。 |
| `MacdCloseLevelPips` | `decimal` | `3` | ポジションを決済するために必要な最小値 MACD の大きさ (ピップ単位)。 |
| `TakeProfitPips` | `decimal` | `550` | ピップス単位で測定されるテイクプロフィット距離。 |
| `TrailingStopPips` | `decimal` | `19` | ピップ単位で測定されるトレーリングストップの距離。値 `0` は末尾を無効にします。 |
| `TradeVolume` | `decimal` | `1` | 市場参入に使用される純量。 |
| `CandleType` | `DataType` | 4時間枠 | 戦略的に加工されたキャンドルシリーズ。 |
| `RequiredSecurityCode` | `string` | `EURUSD` | セキュリティ コードは選択した機器と一致する必要があり、MetaTrader チェックを模倣します。 |

## 元の MetaTrader 専門家との違い
- MetaTrader は、個々の注文とマジックナンバーを管理します。 StockSharp はネット ポジションで動作するため、変換によりポジションがクローズされます。
複数のチケットを操作するのではなく、現在のエクスポージャを取得して新しいチケットを開きます。
- 元のコードでは、位置のサイズを動的に調整するために `AccountFreeMargin` を使用していました。 StockSharp ポートは単純な `TradeVolume` を公開します
ユーザーが外部で位置サイジングを設定する必要があるパラメータとドキュメント。
- ストップロス調整では、既存の注文を変更するのではなく、StockSharp のローソク足の極値を使用します。最初の段階でも終了が発生します
トレーリングストップに違反するローソク足で、MetaTrader ロジックに非常に近い動作を生成します。
- すべてのインジケーターの計算は、`SubscribeCandles` を介してバインドされた StockSharp インジケーター クラスに依存しており、
`iMACD` または `iMA` 関数。

## 使用上の注意
- 戦略を開始する前に、目的の楽器を割り当てます。機器コードが `RequiredSecurityCode` と一致しない場合、
間違った市場への誤った展開を防ぐために、戦略は直ちに停止します。
- `TradeVolume` は `OnStarted` 中に `Strategy.Volume` にコピーされるため、ヘルパー メソッド (`BuyMarket`、`SellMarket`) は常に
設定されたサイズ。
- トレーリングストップは、設定された距離を超えて価格が上昇した場合にのみアクティブになります。それまでの戦略は、
MACD のクロスオーバーとテイクプロフィットの目標。
- チャートに戦略を追加すると、ローソク足、両方のインジケーター、および実行された取引が描画されるため、クロスオーバー ロジックを検証できます。
視覚的に。

## インジケーター
- `MovingAverageConvergenceDivergenceSignal` (MACD ラインと信号ライン)。
- `ExponentialMovingAverage` (トレンドフィルター)。
