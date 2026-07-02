# Zone Recovery Button戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Zone Recovery Button戦略**は、MetaTrader エキスパートアドバイザー "ZONE RECOVERY BUTTON VER1" (`MQL/25347`) を直接変換したものです。元のロボットは、ヘッジされたバスケットを開始するためにチャート上の BUY/SELL ボタンに依存していました。この StockSharp 版では、手動パネルをパラメーターに置き換えながら、回復ロジック、金額/割合 take-profit、通貨単位の trailing stop、equity-stop 保護を維持しています。

戦略が開始方向を受け取ると、初期の成行注文を開きます。価格が設定されたゾーン幅を横切るたびに、システムは増加した数量で反対方向の取引を積み上げます。バスケットは、参照 take-profit に到達したとき、含み益が設定された金額/割合目標に達したとき、trailing stop が利益を過度に戻したとき、または equity-stop しきい値に違反したときに閉じられます。

## 取引ルール

1. **開始方向** - BUY または SELL ボタンの押下をエミュレートします。戦略はデータを受け取り、取引が許可されるとすぐに最初の注文を開きます。バスケットを閉じた後、同じ方向で自動的に再開できます。
2. **ゾーンリカバリー** - 各回復ステップでアルゴリズムは方向を交互に変えます。ロングサイクルでは、価格が `Base Price - Zone Width` を下回ると売り、その後、市場が基準を上回って戻ると再び買います。ショートサイクルではロジックが反転します。
3. **数量スケーリング** - 追加の各ヘッジは、前回数量を乗算するか、固定増分を加え、EA の "Lots"/"Multiply" 設定を再現します。
4. **Take-profit制御** - バスケットは次の条件で閉じられます。
   - 参照価格から測定した pip ベースの take-profit。
   - 口座通貨での金額目標。
   - 現在のポートフォリオ値から計算した割合目標。
   - 含み益がしきい値を超えると利益を固定し、その後許容 drawdown を超えて戻すと閉じる trailing ロジック。
   - 現在の含み損をサイクル中に観測された最高 equity と比較する緊急 equity-stop。

## パラメーター

| パラメーター | デフォルト | 説明 |
|-----------|------------|------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | 価格変動の監視に使用するローソク足タイプ。 |
| `StartDirection` | `Buy` | 初期サイクル方向 (BUY/SELL/NONE)。 |
| `AutoRestart` | `true` | 前のバスケットが閉じた後、新しいサイクルを自動的に再開します。 |
| `TakeProfitPips` | `200` | 基準価格と pip take-profit 目標の間の pip 距離。 |
| `ZoneRecoveryPips` | `10` | 反対方向の次のヘッジを発動する pip 距離。 |
| `InitialVolume` | `0.01` | 最初の取引の数量 (ロット)。 |
| `UseVolumeMultiplier` | `true` | 有効な場合、各ヘッジは前回数量を乗算します。無効な場合は `VolumeIncrement` が追加されます。 |
| `VolumeMultiplier` | `2` | `UseVolumeMultiplier` が `true` のときに適用される乗数。 |
| `VolumeIncrement` | `0.01` | `UseVolumeMultiplier` が `false` のときの数量増分。 |
| `MaxTrades` | `100` | バスケット内の最大取引数。 |
| `UseMoneyTakeProfit` | `false` | 含み益が `MoneyTakeProfit` を超えたときの決済を有効にします。 |
| `MoneyTakeProfit` | `40` | 口座通貨での利益目標。 |
| `UsePercentTakeProfit` | `false` | 含み益が残高の `PercentTakeProfit` パーセントを超えたときの決済を有効にします。 |
| `PercentTakeProfit` | `10` | 現在のポートフォリオ値に対する割合利益目標。 |
| `EnableTrailing` | `true` | 通貨単位の利益 trailing を有効にします。 |
| `TrailingProfitThreshold` | `40` | trailing を起動する利益水準。 |
| `TrailingDrawdown` | `10` | バスケットを閉じる前に許容されるピーク含み益からの drawdown。 |
| `UseEquityStop` | `true` | 緊急 equity stop を有効にします。 |
| `TotalEquityRiskPercent` | `1` | フラット化前に許容される最大含み損 (equity 高値に対する割合)。 |

## 注意事項

- 戦略は `PriceStep` と `StepPrice` の値を提供する任意の商品で動作します。これらのパラメーターは、pip 距離を価格および通貨単位へ変換するために必要です。
- StockSharp はネットポジションモデルを使うため、ヘッジグリッドは内部でシミュレートされます。戦略は MetaTrader の利益計算を再現するため、独自の取引ステップ一覧を保持します。
- trailing ロジックはアクティブなバスケットの含み益で動作します。注文ベースの trailing stop は使用しません。
