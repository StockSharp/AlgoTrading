# JBrainTrend1Stop戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**JBrainTrend1Stop戦略**は、MetaTrader 5エキスパートアドバイザー`Exp_JBrainTrend1Stop`のStockSharpポートです。2つのAverage True Range測定値、Stochasticオシレーター、JurikMovingAverageを組み合わせてBrainTradingのトレンド反転を検出します。Jurikで平滑化された価格が十分に大きなスイングを作り、Stochasticがニュートラルゾーンから抜け出ると、戦略はバイアスを切り替え、BrainTrendのストップラインを更新し、設定可能な遅延後に（オプションで）ネットポジションを反転します。

## トレードロジック

1. `CandleType`で定義されたローソク足を購読し、以下に入力します：
   - `AtrPeriod`の長さを持つ主要`AverageTrueRange`。
   - `AtrPeriod + StopDPeriod`の期間を持つ拡張`AverageTrueRange`。
   - `StochasticPeriod`と1バー%K平滑化（MT5設定に対応）の`StochasticOscillator`。
   - `JmaLength`と`JmaPhase`で設定した3つの`JurikMovingAverage`インスタンス（高値、安値、終値）。
2. 各確定ローソク足で計算：
   - `range = ATR / 2.3`（元の定数`d = 2.3`に対応）。
   - `range1 = ATR_extended * 1.5`（`s = 1.5`に対応）。
   - `val3 = |JMA_close - JMA_close[shift 2]|`（MT5バッファ差を再現）。
3. `val3 > range`かつStochasticがニュートラルバンドを抜け出す時：
   - `%K < 47`の場合、戦略は弱気BrainTrend状態（`_trendState = -1`）に入り、売りストップを`JMA_high + range1 / 4`に設定して**売り**シグナルを生成。
   - `%K > 53`の場合、戦略は強気状態（`_trendState = 1`）に入り、買いストップを`JMA_low - range1 / 4`に設定して**買い**シグナルを生成。
4. 状態が変わらない間、BrainTrendストップは`range1`だけ価格に向かってトレールします（弱気トレンドでは`JMA_high + range1`、強気トレンドでは`JMA_low - range1`）。
5. シグナルは`SignalBar`本の確定バー後に解放されます。実行時：
   - 買いシグナルはショートポジションをクローズし（`SellClose`が有効な場合）、オプションで新しいロングを建てます（`BuyOpen`が有効な場合）。
   - 売りシグナルはロングポジションをクローズし（`BuyClose`が有効な場合）、オプションで新しいショートを建てます（`SellOpen`が有効な場合）。

チャートはJurikで平滑化した終値とStochasticオシレーターをトレードマーカーと共に自動表示します。

## パラメーター

| パラメーター | 説明 | デフォルト |
|-------------|------|-----------|
| `CandleType` | 戦略が処理するローソク足シリーズ。 | H4（4時間足） |
| `AtrPeriod` | BrainTrendトリガーに使用する主要ATRの長さ。 | 7 |
| `StochasticPeriod` | Stochasticオシレーターの%K/%D期間（1バー%K平滑化）。 | 9 |
| `StopDPeriod` | 第2ATR期間に追加するバー数（`AtrPeriod + StopDPeriod`）。 | 3 |
| `JmaLength` | 高値/安値/終値に適用するJurik移動平均の長さ。 | 7 |
| `JmaPhase` | Jurik移動平均に渡すフェーズ引数（[-100; 100]に制限）。 | 100 |
| `SignalBar` | 新しいシグナルを発火する前に待つ確定バーの数。 | 1 |
| `BuyOpen` / `SellOpen` | シグナル後にロング/ショートポジションに入ることを許可。 | `true` |
| `BuyClose` / `SellClose` | 反対シグナルで既存のロング/ショートポジションのクローズを許可。 | `true` |

注文サイズの制御には戦略の`Volume`プロパティまたはブローカーの設定を使用します。

## MT5バージョンとの違い

- 元のマネー管理ブロック（`MM`、`MMMode`、`Deviation_`、動的ロットサイジング）はStockSharpの標準注文サイジング（`Volume`と成行注文）に置き換えられています。スリッページ制御は再現されていません。
- 絶対的なストップロス・テイクプロフィット距離（`StopLoss_`、`TakeProfit_`）は実装されていません。必要であれば、ホスティング環境を通じて手動で保護を設定できます。
- BrainTrendのストップレベルはシグナルタイミングのために内部的に使用されます；未決注文としては設定されません。
- Jurik移動平均はStockSharpの`JurikMovingAverage`実装に依存します。フェーズパラメーターはリフレクションで適用され、このリポジトリの他のBrainTradingポートの動作と一致します。

## 使用方法

1. 戦略をアセットに接続し、`CandleType`を設定します（例：EAとの一貫性のために4時間ローソク足）。
2. インジケーターパラメーター（`AtrPeriod`、`StochasticPeriod`、`StopDPeriod`、`JmaLength`、`JmaPhase`）を調整して、希望するBrainTrend感度に合わせます。
3. 必要に応じてシグナル実行を何本かの確定バーで遅延させるために`SignalBar`を調整します。
4. 好みのトレード方向を反映するために`Volume`とオープン/クローズのトグルを設定します。
5. （オプション）ホスティングプラットフォームを通じてストップロスやポートフォリオ制限などの外部リスク管理を追加します。

実行開始後、戦略はBrainTrendの反転を追跡し、反対ポジションをクローズし、設定した遅延後にオプションで方向を転換します。
