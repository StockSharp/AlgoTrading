# Stochastic Momentum Filter 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Stochastic Momentum Filter 戦略**は、MetaTraderのエキスパートアドバイザー`Stochastic.mq4`（フォルダ`MQL/23473`）のStockSharpポートです。元のロボットは2つのストキャスティクスオシレーター、線形加重移動平均（LWMA）、Momentumの偏差フィルター、および上位タイムフレームのMACDトレンドチェックを組み合わせます。このC#バージョンはStockSharpの高レベルAPIの上で同じ構成要素を再現し、多層確認ワークフローを維持します：

1. **トレンドフィルター** – ロングトレード（またはショートトレード）が許可される前に、速いLWMAが遅いLWMAより上（または下）にある必要があります。
2. **オシレーター確認** – 速いストキャスティクス（5/2/2）と遅いストキャスティクス（21/4/10）の両方が売られ過ぎ/買われ過ぎゾーンで一致する必要があります。
3. **Momentumの偏差** – 最近の3つのMomentum読み取り値のうち少なくとも1つが設定可能な閾値を超えて100のベースラインから偏差しなければならず、エキスパートによるMT4の`iMomentum`関数の使用に対応します。
4. **上位タイムフレームMACD** – 設定可能な上位タイムフレームのMACDメインラインはロングのシグナルラインより上（ショートでは下）に留まる必要があります。デフォルトの30日タイムフレームは元の月次フィルターに近似します。
5. **リスクロジック** – ストップロス、テイクプロフィット、およびオプションのトレーリングは`StartProtection`を通じて処理され、EAの保護パラメーターを反映します。ポジションのフリップは新しいネットポジションを確立する前に反対の露出を自動的に閉じます。

戦略は2つのローソク足ストリームを購読します：トレードタイムフレームとMACDフィルターを供給する上位タイムフレーム。すべての計算はStockSharpインジケーターで実行され、高レベルの`Bind`ヘルパーを通じて処理されます。

## パラメーター
| 名前 | デフォルト | 説明 |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | ロングセットアップのために両方のストキャスティクスオシレーターが破るべき売られ過ぎレベル。 |
| `StochasticSellLevel` | `80` | ショートセットアップのために両方のストキャスティクスオシレーターが達するべき買われ過ぎレベル。 |
| `FastMaPeriod` | `6` | 速いLWMAトレンドフィルターの長さ。 |
| `SlowMaPeriod` | `85` | 遅いLWMAトレンドフィルターの長さ。 |
| `FastStochasticPeriod` | `5` | 速いストキャスティクスオシレーターの`%K`期間。 |
| `FastStochasticSignal` | `2` | 速いストキャスティクスの`%D`スムージング期間。 |
| `FastStochasticSmoothing` | `2` | 速いストキャスティクスに適用される追加スムージング（MT4の「slowing」に対応）。 |
| `SlowStochasticPeriod` | `21` | 遅いストキャスティクスオシレーターの`%K`期間。 |
| `SlowStochasticSignal` | `4` | 遅いストキャスティクスの`%D`スムージング期間。 |
| `SlowStochasticSmoothing` | `10` | 遅いストキャスティクスに適用される追加スムージング。 |
| `MomentumPeriod` | `14` | Momentumオシレーターのルックバック（MT4の`iMomentum`と同じ）。 |
| `MomentumThreshold` | `0.3` | 最後の3つのMomentum値内で必要な100ベースラインからの最小絶対偏差。 |
| `MacdFastPeriod` | `12` | 上位タイムフレームMACDの速いEMA期間。 |
| `MacdSlowPeriod` | `26` | 上位タイムフレームMACDの遅いEMA期間。 |
| `MacdSignalPeriod` | `9` | 上位タイムフレームMACDのシグナルEMA期間。 |
| `TakeProfitPoints` | `50` | テイクプロフィット距離（価格ポイント）。無効にするには`0`に設定。 |
| `StopLossPoints` | `20` | ストップロス距離（価格ポイント）。無効にするには`0`に設定。 |
| `EnableTrailing` | `true` | 保護ストップのStockSharpトレーリングを有効にする。 |
| `TradeVolume` | `1` | 各シグナルでターゲットとするネットポジションサイズ。 |
| `MaxNetPositions` | `1` | スタックされたネット露出を制限する（`TradeVolume`を乗算）。 |
| `CandleType` | `15m`タイムフレーム | メインのトレードタイムフレーム。 |
| `HigherTimeframe` | `30d`タイムフレーム | MACD確認に使用するタイムフレーム。 |

## トレードロジック
1. **インジケーターの準備** – 戦略は両方のLWMA、両方のストキャスティクスオシレーター、Momentumインジケーター、およびMACDをそれぞれのローソク足ストリームにバインドします。
2. **Momentumの履歴** – Momentumオシレーターの100からの絶対距離は最後の3本の完了したバーに保存されます。これはEAの`MomLevelB/MomLevelS`配列を再現します。
3. **エントリールール**
   - **ロング**：速いLWMAが遅いLWMAより上、両方のストキャスティクスの`%K`と`%D`値が`StochasticBuyLevel`より下、Momentumの偏差が`MomentumThreshold`より上、そしてMACDメインラインがシグナルラインより上。
   - **ショート**：速いLWMAが遅いLWMAより下、両方のストキャスティクスの`%K`と`%D`値が`StochasticSellLevel`より上、Momentumの偏差が閾値より上、そしてMACDメインラインがシグナルラインより下。
4. **ポジション処理** – 注文は`BuyMarket`/`SellMarket`で送信されます。反転シグナルが現れると、戦略は新しい方向を確立する前に反対のネット露出を自動的に閉じます。
5. **保護** – `StartProtection`は設定されたテイクプロフィットとストップロス距離（ポイント）を適用します。`EnableTrailing`がtrueの場合、StockSharpはEAのトレーリングルーティンと同様にストップのトレーリングを管理します。

## MQLバージョンとの違い
- **ボリュームスケーリング**：EAは`LotExponent`を使用してロットサイズをスケールし、複数の同時チケットを許可します。StockSharpポートはネット露出に焦点を当て、方向ごとに単一の`TradeVolume`をターゲットとします（`MaxNetPositions`で制限）。
- **マージン管理**：元のスクリプトのマージンチェック、エクイティストップ、および通知機能はMT4アカウントAPIに依存するため再現されません。
- **フリーズレベル**：ブローカー固有の低レベルフリーズレベルチェックは省略されます；StockSharpの注文ルーティングが取引所の制約を処理します。
- **ブレークイーブントグル**：MT4の「break-evenへ移動」ヘルパーはStockSharpのトレーリング保護に置き換えられます。

## 使用上の注記
1. 証券とコネクターを割り当て、戦略を開始します。トレードタイムフレームとMACDフィルターに必要な上位タイムフレームの両方を自動的に購読します。
2. データソースが30日のローソク足タイプをサポートしない場合は、`HigherTimeframe`をサポートされている間隔（例：週次または日次）に調整してください。
3. ポートフォリオユニットに合わせて`TradeVolume`を設定します。
4. 保護注文を無効にする場合は`TakeProfitPoints`/`StopLossPoints`をゼロに設定します。
5. コード内のすべてのコメントは英語で記述され、インデントにはタブを使用します。

## ファイル
- `CS/StochasticMomentumFilterStrategy.cs` – 戦略ロジックのStockSharp実装。
- `README.md` – 英語ドキュメント（このファイル）。
- `README_ru.md` – ロシア語ドキュメント。
- `README_zh.md` – 中国語ドキュメント。
