# Burg 外挿予測戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Burg Extrapolator 戦略は、MetaTrader 4 エキスパート アドバイザー「Burg Extrapolator」の StockSharp 移植です。オリジナルのシステムは、ブルク自己回帰 (AR) モデルを始値のスライディング ウィンドウ (またはその勢い/ROC 変換) に適合させ、将来の価格の経路を予測します。トレーディングの決定は、最も極端な予測値に基づいて行われます。つまり、一方向への予測変動が十分に大きい場合、戦略は新しいポジションを積み上げるか、反対方向へのエクスポージャーを清算します。変換では同じモデリング ブロックが維持され、ポジション管理と資金管理が StockSharp プリミティブにマッピングされます。

## 取引ロジック
1. **データの準備**
   - 選択した `CandleType` の `PastBars + 1` の始値のローリング履歴を作成します。
   - オプションで、AR モデルにデータを供給する前に、データを対数運動量 (デフォルト) または変化率に変換します。 Raw prices are centered by their moving average to mirror the MT4 code.
2. **バーグ線形予測**
   - Burg アルゴリズムを使用して、`PastBars * ModelOrder` 次数までの反射係数を推定します。
   - AR モデルを再帰的に拡張することで、一連の将来の値 (実際には `PastBars` ステップ先) を生成します。変換は逆に価格空間に戻されるため、すべての予測は絶対価格単位で機能します。
3. **信号検出**
   - 予測パスをたどって、別の極端な値が現れる前に予測最高値と最低価格を記録します。最初の極値と予測範囲の反対側の間の距離は、`MaxLoss` および `MinProfit` のしきい値と比較されます (商品 `PriceStep` を乗算することで絶対価格に変換されます)。
   - 十分に大きな上昇は `OpenSignal = 1` をトリガーし、大きな下降は `OpenSignal = -1` を生成します。 If the opposing extreme appears first the logic sets `CloseSignal` to exit current exposure even if no fresh entry is planned.
4. **注文管理**
   - 新しいシグナルが実行される前に、保護的出口 (ストップロス、テイクプロフィット、およびオプションのトレーリングストップ) が実行されます。トレーリングストップは最後のエントリー以降の最良価格を再利用し、価格が `TrailingStop` ポイントまでリトレースしたときにポジションを閉じます。これは、保護注文を移動する MT4 の動作と一致します。
   - If a signal asks to close exposure in the opposite direction the strategy sends a market order sized to flatten the current net position.
   - エントリーシグナルは、`MaxTrades` に達するまで、示された方向に追加の成行注文を積み上げます。注文量は、元の証拠金ベースのサイジング ルーチンの StockSharp に適した置き換えである係数 `1 + existingTrades * MaxRisk` を使用して、アクティブな取引の数に比例して増加します。

## 指標とデータ
- Candle subscription defined by `CandleType` (default 30-minute time frame).
- Internal Burg autoregressive model (implemented without external indicators).
- オプションの対数運動量と変化率の変換。

## パラメーター
| 名前 | デフォルト | 説明 |
| --- | --- | --- |
| `CandleType` | 30分キャンドル | 戦略によって処理される主な時間枠。 |
| `MaxRisk` | 0.5 | 複数の取引を積み重ねるときに使用されるリスク乗数。 |
| `MaxTrades` | 5 | 方向ごとの同時取引の最大数。 |
| `MinProfit` | 160 | Minimum predicted profit (in points) required to open new trades. |
| `MaxLoss` | 130 | Maximum tolerated forecasted loss (in points) before closing trades. |
| `TakeProfit` | 0 | オプションの固定テイクプロフィット距離 (ポイント単位) (0 は無効にします)。 |
| `StopLoss` | 180 | オプションの固定ストップロス距離 (ポイント単位) (0 は無効にします)。 |
| `TrailingStop` | 10 | Trailing stop distance in points, active only when `StopLoss > 0`. |
| `PastBars` | 200 | Number of historical candles used by the Burg model. |
| `ModelOrder` | 0.37 | `PastBars` の一部がバーグ注文に変換されました。 |
| `UseMomentum` | 本当の | Apply logarithmic momentum transform to input data. |
| `UseRateOfChange` | 偽 | Apply percentage rate of change (ignored when momentum is enabled). |

All parameters are `StrategyParam<T>` instances and can be optimised or adjusted in the StockSharp Designer.

## 実装メモ
- Burg アルゴリズムは C# で直接実装され、MT4 バージョンと同じ再帰を維持します。すべての計算は倍精度で実行され、最終予測は信号チェックの前に `decimal` に変換されます。
- The original EA could rely on MetaTrader account information to size positions. StockSharp では、資金管理ブロックが、`Volume` と `MaxRisk` に基づく決定的なスケーリング ルールに置き換えられます。 Set `Volume` to the desired base lot and the strategy will scale subsequent entries proportionally.
- 保護ロジックは、ブローカー側のストップを変更するのではなく、明示的な成行注文でポジションをクローズします。これは、StockSharp の高レベルの API 設計と一致し、シミュレーションでの実行時に古い状態になるのを防ぎます。
- 予測配列は、`PastBars` または `ModelOrder` が変更されるたびに再作成されるため、戦略を再起動することなく、オンザフライのパラメーター編集が AR モデルに即座に影響します。
- To visualise the behaviour you can attach a chart in Designer: the strategy already draws candles and executed trades on the default area. Extending the sample with custom series (e.g., forecast path) is straightforward if desired.
