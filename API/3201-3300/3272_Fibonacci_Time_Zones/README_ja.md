# Fibonacci Time Zones戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、MetaTrader のエキスパートアドバイザー "Fibonacci Time Zones" を StockSharp に移植したものです。上位時間枠の MACD フィルター、Bollinger バンドによるエグジット、充実した資金管理モジュールを組み合わせることで、元のスクリプトの裁量的な特徴を保っています。すべての取引管理ルーチンは高レベル API を使って書き直されています。戦略は 2 つのローソク足ストリーム (取引用時間枠と MACD 確認用の低速時間枠) を購読し、`Bind`/`BindEx` コールバックでインジケーターを直接バインドします。

## 中核ロジック

1. **Momentumフィルター** - 月足 (設定可能) の MACD ヒストグラムを計算します。シグナルラインを上抜ける強気クロスはロングエントリーを予約し、弱気クロスはショートエントリーを予約します。同じクロスで注文が繰り返されるのを避けるため、実際のポジションは次の取引用ローソク足で開かれます。
2. **エントリー実行** - 各シグナルは、ユーザー定義数の成行注文を送信します。新しいポジションを開く前に、既存の反対方向エクスポージャーを解消します。
3. **エグジットルール** - 複数の防御層が適用されます。
   - **Bollingerバンド・エグジット**: ロングは価格が上側バンドに触れたとき、ショートは下側バンドに到達したときに決済されます。
   - **クラシックなストップ/目標**: 固定の stop-loss、take-profit、trailing-stop 距離は pips から価格単位に変換され、`StartProtection` に渡されます。
   - **Break-even**: 価格が設定可能な pips 数だけ進むと、ストップは break-even にオフセットを加えた位置へ引き上げられます。価格がその水準まで戻るとポジションを決済します。
   - **金額ベースのtrailing**: 未決済 PnL と実現 PnL を監視します。含み益がしきい値に達すると、戦略はその利益を trail し始め、設定可能な drawdown 後にすべてを決済します。
   - **Equity目標**: 任意の絶対額または割合の利益目標に達すると、すべての取引を直ちに決済します。

## パラメーター

| パラメーター | 説明 |
|-----------|------|
| `UseTakeProfitMoney`, `TakeProfitMoney` | 合計利益 (実現 + 未実現) が指定した口座通貨額に達したとき、すべてのポジションを決済します。 |
| `UseTakeProfitPercent`, `TakeProfitPercent` | 前のオプションと似ていますが、開始時 equity に対する割合で測定します。 |
| `EnableTrailingProfit`, `TrailingTakeProfitMoney`, `TrailingStopLossMoney` | 最初のしきい値に達した時点で金額ベースの trailing を有効にし、蓄積した利益を保護します。 |
| `UseStop`, `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | pips で表されるクラシックなストップ、目標、trailing 距離。 |
| `UseMoveToBreakEven`, `WhenToMoveToBreakEven`, `PipsToMoveStopLoss` | break-even 動作を制御します。 |
| `NumberOfTrades` | 各シグナルで送信される成行注文数 (エントリーを積み増せた元の EA を模倣)。 |
| `CandleType`, `MacdCandleType` | 管理用ローソク足と MACD フィルターの時間枠。 |

## 元のEAとの違い

* チャートボタン処理とグラフィカルな Fibonacci オブジェクトは再現していません。StockSharp 版は純粋に体系的な実行に集中しています。
* 元のエキスパートは手動ボタンクリックで取引していました。この移植版は MACD クロスで自動的にエントリーし、決定論的でバックテスト可能な戦略にしています。
* MetaTrader 固有の口座関数は、StockSharp の同等機能 (`Portfolio` 値と `PnL`) に置き換えられました。

## 使用のヒント

1. 戦略を開始する前に、適切なローソク足タイプを選択してください。デフォルトは、月足 MACD フィルターを持つ 15 分足の取引チャートに対応します。
2. 商品の tick サイズに合わせて pip ベースの距離を調整します。戦略は内部で `Security.PriceStep` を使い、pips を価格に変換します。
3. 裁量的に介入する場合は、自動利益目標を無効にし、Bollinger エグジットのみを使用します。
