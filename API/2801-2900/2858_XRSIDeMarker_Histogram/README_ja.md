# XRSI DeMarker ヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はエキスパートアドバイザー **Exp_XRSIDeMarker_Histogram** を再現します。相対強度指数（RSI）とDeMarkerインジケーターを組み合わせ、結果を平滑化するカスタムオシレーターで検出された反転を取引します。システムはロングとショートのトレードを独立して開閉でき、価格ステップで表現されたオプションの保護ストップをサポートします。

## インジケーターの構成
1. **適用価格** – RSIは設定された期間を使用して選択された入力（終値、始値、高値、安値、中値、典型的または加重価格）で計算されます。
2. **DeMarkerコンポーネント** – 完成した各ローソク足について、戦略は上昇（`deMax`）と下降（`deMin`）圧力を測定します：
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   両方のシリーズはRSI期間と同じ長さの単純移動平均で平滑化されます。
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)`（0–100の範囲にスケーリング）。
3. **複合オシレーター** – 最終値は`(RSI + 100 * DeMarker) / 2`です。
4. **平滑化** – 複合オシレーターはサポートされている移動平均（SMA、EMA、SMMA、LWMA、またはJurik）の1つを通過します。オリジナルMQLバージョンからサポートされていない平滑化モードが選択された場合、インジケーターは要求された長さのEMAにフォールバックします。Jurikオプションはフェーズパラメーターも考慮します。
5. **シグナル履歴** – 戦略は履歴値を保存し、`SignalBar`で定義されたバーのシグナルを評価します。これは取引前に次のローソク足を待つオリジナルEAを模倣します。

## トレードロジック
- **強気の反転**
  - 条件：`SignalBar+1`の値が`SignalBar+2`より低く（下降傾向）、`SignalBar`の値が再び上昇（`>=`）。
  - アクション：
    - `CloseShortOnLongSignal`が真の場合、既存のショートトレードを閉じます。
    - `AllowBuyEntries`が有効な場合、`TradeVolume`で新しいロングトレードを開きます（ショートからの反転に必要な数量を加算）。
- **弱気の反転**
  - 条件：`SignalBar+1`の値が`SignalBar+2`より高く（上昇傾向）、`SignalBar`の値が下降（`<=`）。
  - アクション：
    - `CloseLongOnShortSignal`が真の場合、既存のロングトレードを閉じます。
    - `AllowSellEntries`が有効な場合、新しいショートトレードを開きます。
- インジケーターとDeMarkerコンポーネントが完全に形成されるまでシグナルは無視され、戦略がオンラインでトレードが許可されている場合のみ注文が置かれます。

## リスク管理
- `StopLossTicks`と`TakeProfitTicks`は**価格ステップ**の距離を表します。戦略はこれらの値を`Security.PriceStep`で掛けます（インストゥルメントステップが不明な場合は`1`にフォールバック）、ローソク足の範囲内で距離に達したときにポジションを閉じます。
- `0`を渡すと対応する保護が無効になります。
- `TradeVolume`パラメーターはデフォルトの注文サイズとして使用され、反転の計算にも使われます（反対のポジションが新しいポジションを開く前に閉じられます）。

## パラメーター
| パラメーター | 説明 | デフォルト値 |
|-----------|-------------|---------|
| `TradeVolume` | 新しいポジションを開く際のボリューム。 | `0.1` |
| `StopLossTicks` | 価格ステップでの保護ストップ。 | `1000` |
| `TakeProfitTicks` | 価格ステップでの利益目標。 | `2000` |
| `AllowBuyEntries` | ロングトレードの有効化/無効化。 | `true` |
| `AllowSellEntries` | ショートトレードの有効化/無効化。 | `true` |
| `CloseLongOnShortSignal` | ショートシグナルが表示されたときにロングを閉じる。 | `true` |
| `CloseShortOnLongSignal` | ロングシグナルが表示されたときにショートを閉じる。 | `true` |
| `CandleType` | 分析に使用するタイムフレーム（デフォルト4時間ローソク足）。 | `H4` |
| `IndicatorPeriod` | RSIとDeMarkerコンポーネントのルックバック。 | `14` |
| `AppliedPriceSelection` | RSI計算で使用される適用価格。 | `Close` |
| `SmoothingMethodSelection` | 平滑化に使用する移動平均（SMA/EMA/SMMA/LWMA/Jurik/Adaptive）。 | `Sma` |
| `SmoothingLength` | 平滑化平均の期間。 | `5` |
| `SmoothingPhase` | Jurik平滑化に渡されるフェーズ引数。 | `15` |
| `SignalBar` | シグナル評価に使用される閉じたバーの数。 | `1` |

## オリジナルEAとの相違点
- MQLバージョンの資金管理モード（残高ベース、フリーマージンベースなど）は直接の`TradeVolume`パラメーターに置き換えられます。
- StockSharpは成行注文を使用するため、注文スリッページ（`Deviation`）は不要です。
- 高度な平滑化アルゴリズム（放物線MA、T3、VIDYA、AMA）はStockSharpでは利用できず、`Adaptive`オプションを介してEMAにマッピングされます。
- C#ソースコードのすべてのコメントは英語で書かれており、ロジックはオリジナルの実装と同様に完成したローソク足のみで実行されます。
