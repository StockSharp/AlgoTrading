# RRS インパルス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**RRS Impulse Strategy** は、MetaTrader エキスパート アドバイザー「RRS Impulse」の高レベルの StockSharp 移植です。オリジナルロボット
RSI、Stochastic、および Bollinger バンド フィルターを組み合わせ、いくつかの信号強度モード間でローテーションし、保護ストップを使用し、
仮想後続出口。この C# バージョンは同じ動作を維持しますが、純粋に StockSharp の高レベル API: Candle に依存します。
サブスクリプションはインジケーターをフィードし、`BuyMarket`、`SellMarket`、および `ClosePosition` は注文を実行します。

## 取引ロジック

1. **インジケーター モード** – 4 つのオプションから選択します。
   - `Rsi`: 買われ過ぎ/売られ過ぎゾーンを抜けたときにオシレーターを取引します。
   - `Stochastic`: %K と %D の両方が構成されたレベルより上または下である必要があります。
   - `BollingerBands`: 上部バンドを上回る、または下部バンドを下回る終値に反応します。
   - `RsiStochasticBollinger`: 3 つのフィルターすべてが同じ方向を確認した場合にのみ起動します。
2. **取引方向** – `Trend` はインジケーターに従います (買われすぎはショートに、売られすぎはロングにつながります)。 `CounterTrend` がフェードアウトします
動きます（買われすぎはロングを引き起こし、売られすぎはショートを引き起こします）。
3. **信号強度** – 取引を開始する前に一致する必要があるタイムフレームの数を制御します。
   - `SingleTimeFrame`: `CandleType` によって提供される基本タイムフレームのみを使用します。
   - `MultiTimeFrame`: M1、M5、M15、M30、H1、および H4 ローソク足全体での位置合わせが必要です。
   - `Strong`: M1、M5、M15、M30 をチェックして、日中の勢いに焦点を当てます。
   - `VeryStrong`: M1 … H4 ラダー全体からの確認を要求します。複合インジケーターモードが時間枠ごとに有効になっている場合
3 つのフィルター *すべて* を満たす必要があります。
4. **リスク管理** – 各ポジションは平均約定価格を追跡し、次の 3 つのエグジット条件を監視します。
   - ピップ単位での固定ストップロス距離。
   - ピップ単位で固定されたテイクプロフィット距離。
   - 利益が `TrailingStartPips` を超えるとトレーリング ストップが有効になり、`TrailingGapPips` まで維持されます。
方向が反転するたびに、戦略は最初に `ClosePosition()` を呼び出してフラット化し、その後にのみ反対の取引を開始します。
次の確認チェックマーク。

## パラメーター

| グループ       | 名前 | 説明 |
|-------------|------|-------------|
| データ        | `CandleType` | 取引決定のために処理されたベースキャンドルシリーズ。 |
| 注文      | `TradeVolume` | 成行注文を送信するときに使用される数量。 |
| リスク        | `StopLossPips`, `TakeProfitPips`, `TrailingStartPips`, `TrailingGapPips` | 仮想保護出口はピップで表されます。 |
| 信号     | `IndicatorMode`, `TradeDirection`, `SignalStrength` | MQL 入力ブロックからコピーされた動作スイッチ。 |
| RSI         | `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | 買われ過ぎ/売られ過ぎを検出するための RSI 構成。 |
| Stochastic  | `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing`, `StochasticUpperLevel`, `StochasticLowerLevel` | 遅い確率的オシレーター設定。 |
| Bollinger   | `BollingerPeriod`, `BollingerDeviation` | Bollinger バンドのルックバックと偏差乗数。 |

すべてのパラメータは、意味のある MetaTrader バージョンと同じ最適化範囲をサポートします (例: 停止して距離を取るなど)
または発振器のしきい値）。

## データ要件

戦略には確認ラダー用の微細なキャンドルが必要です。 `SignalStrength` が追加の時間枠をリクエストすると、戦略は
必要なサブスクリプションを自動的に追加します (`GetWorkingSecurities` はそれらをエンジンにアドバタイズします)。レベル 1 の引用符は使用されません。
完成したローソク足の終値だけがエントリーとエグジットを左右します。したがって、保護ロジックは「仮想」ストップ/ターゲットを再現します。
オリジナルロボットの動作。

## 変換時の注意点

- EA からのランダムなシンボルの回転は意図的に削除されました。 StockSharp 戦略は単一の `Security` で機能するため、
ポートは、ユーザーに機器のローテーションを任せながら、インジケーターのロジックの一致とリスク管理に集中します。
- 注文管理は市場ベースです。方向が変わるか、保護条件がトリガーされると、`ClosePosition()` が呼び出されます。
チケットを反復処理する MetaTrader ループをミラーリングします。
- 変換では、すべてのコメントが英語のままになり、リポジトリのガイドラインに準拠するためにインデントにタブが使用されます。
