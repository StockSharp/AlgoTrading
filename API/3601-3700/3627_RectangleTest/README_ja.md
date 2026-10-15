# 長方形のテスト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Rectangle Test 戦略は、StockSharp の高レベルの API を使用して、MetaTrader の「RectangleTest」エキスパートを再現します。日中の時間枠で横方向のレンジを検出し、2 つの移動平均と現在の価格が検出されたレンジ内にあるかどうかを確認し、長方形からより速い方の EMA の方向にブレイクアウトします。すべてのロジックは、構成可能なキャンドル ソースから受信した完了したキャンドルで実行されます。

## 取引ロジック
1. プライマリローソク足ストリーム (デフォルト: 1 時間の時間枠) をサブスクライブし、それを次のインジケーターにフィードします。
   - **ExponentialMovingAverage (EMA)**、構成可能な長さは `EmaPeriod` です。
   - **SimpleMovingAverage (SMA)**、構成可能な長さは `SmaPeriod` です。
   - 長さが `RangeCandles` の **最高値** および **最低値** インジケーター。ローソク足の高値と安値を読み取るように構成されています。これらは、MetaTrader 配列ベースの計算をエミュレートする四角形の境界を提供します。
2. すべてのインジケーターが形成されたら、上部境界線に対する長方形の高さをパーセントで計算します。高さが `RectangleSizePercent` より小さいローソク足のみが有効な統合とみなされます。
3. EMA、SMA、およびローソク足が長方形の内側に留まるように閉じる必要があります。これは、MQL バージョンの横フィルターを再現します。
4. **簡単なセットアップ**:
   - EMA は SMA の上にあります。
   - 終値は EMA を上回っています (完了したローソク足の MetaTrader からの「Ask > EMA」条件に一致します)。
   - 既存のロングの任意清算が最初に行われ、その後、ショート成行注文が送信されます。
5. **長いセットアップ**:
   - EMA は SMA の下にあります。
   - 終値が EMA を下回っています (「入札 < EMA」ルールを反映しています)。
   - 既存のショートはロングをオープンする前に清算されます。
6. すべてのエントリには、予想されるエントリ価格とボリュームが記録されます。ポジションがゼロに達すると、ストラテジーはエグジット価格と保存されているエントリー価格を比較します。取引に負けると毎日の損失カウンターが増加し、MQL ヘルパー `Loss()` とまったく同じように `MaxLosingTradesPerDay` フィルターが適用されます。

## お金とリスクの管理
- この戦略は 2 つのモードで機能します。
  - **リスクベース モード** (`UseRiskMoneyManagement = true`): ポジション量は、アカウント値、`RiskPercent`、および設定された `StopLossPoints` から決定されます。計算では、`Security.PriceStep`、`Security.StepPrice`、および `Security.VolumeStep` を使用して、MetaTrader ロットサイジング ルーチンをミラーリングします。
  - **固定ボリュームモード** (`UseRiskMoneyManagement = false`): 取引では `FixedVolume` パラメータが使用されます。
- ネットポジションがフラットからゼロ以外に変化した後、`SetStopLoss` と `SetTakeProfit` は、元のエキスパートで `m_trade.Sell/Buy` に渡された SL/TP 距離と一致する、`StopLossPoints` と `TakeProfitPoints` (価格ステップで表現) を使用して保護注文を登録します。
- `MaxLosingTradesPerDay` は、指定された数の負け取引が検出されると、その日の残りの間、新しいシグナルを停止します。

## 時間管理
- 取引は `TradeStartTime` と `TradeEndTime` の間でのみ許可されます。ヘルパーは、日中のセッションだけでなく、深夜にまたがる間隔も処理します。
- `EnableTimeClose` が true の場合、すべてのオープン ポジションは `TimeClose` の後に清算され、MetaTrader の "TimeCloseTrue" および `TimeClose` の入力が複製されます。

## MetaTrader バージョンとの違い
- 元のインジケーターはチャート上にグラフィカルな四角形を作成しました。 StockSharp は描画オブジェクトを作成しません。代わりに、同じ範囲が最高/最低インジケーターを介して内部的に計算されます。
- 損失取引はシグナルローソク足の終値を使用してカウントされます。これは、高レベルの StockSharp 抽象化の範囲内に留まりながら、`Loss()` の意図（1 日あたりの損失注文のカウント）と一致します。
- `ORDER_FILLING_FOK/IOC` などの注文充填特性は StockSharp の環境によって処理されるため、明示的な充填モード構成は必要ありません。

## パラメーター
| 名前 | デフォルト | 説明 |
| ---- | ------- | ----------- |
| `EmaPeriod` | 45 | 高速の EMA の期間。 |
| `SmaPeriod` | 200 | 遅い SMA の期間。 |
| `RangeCandles` | 10 | 長方形を形成するキャンドルの数。 |
| `RectangleSizePercent` | 0.5 | 取引に許可される長方形の最大の高さ。 |
| `StopLossPoints` | 250 | 価格ステップのストップロス距離。 |
| `TakeProfitPoints` | 750 | 価格ステップでの利食い距離。 |
| `UseRiskMoneyManagement` | 本当の | リスクベースと固定量を切り替えます。 |
| `RiskPercent` | 1 | 取引ごとにリスクが生じる口座資本の割合。 |
| `FixedVolume` | 1 | リスクベースのサイジングが無効になっている場合の固定ボリューム。 |
| `MaxLosingTradesPerDay` | 1 | 損失トレードの毎日の上限。 |
| `TradeStartTime` | 03:00 | エントリーが許可される時間帯。 |
| `TradeEndTime` | 22:50 | 新しいエントリが生成されなくなる時刻。 |
| `EnableTimeClose` | 偽 | 一日の終わりの清算を可能にします。 |
| `TimeClose` | 23:00 | すべてのポジションを決済する時刻。 |
| `CandleType` | 1時間キャンドル | プライマリローソク足データソース。 |

## グラフ化
チャート領域が利用可能な場合、戦略は価格ローソク足、速い EMA、遅い SMA、および独自の取引を描画して、レンジブレイクアウトと取引タイミングを視覚化します。
