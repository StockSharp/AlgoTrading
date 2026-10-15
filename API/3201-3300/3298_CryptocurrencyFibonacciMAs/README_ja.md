# Cryptocurrency Fibonacci MAs戦略 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTrader エキスパートアドバイザー "Cryptocurrency Fibonacci MAs" を StockSharp の高レベル API に移植します。システムは Fibonacci ベースの指数移動平均スタック (8/13/21/55) を追跡し、上位時間枠で momentum を検証し、成行注文を送信する前に月次 MACD フィルターでマクロトレンドを確認します。確定ローソク足のみが処理され、すべてのインジケーター更新は `Bind`/`BindEx` パイプラインで実行されます。

MetaTrader 版と比較して、次の意図的な調整が行われました。
- 金額ベースの take profit、equity stop-out、ローソク足ごとの trailing、break-even 自動化は省略されています。StockSharp 版は `StartProtection` を通じて、クラシックな pip ベースの stop-loss と take-profit を使います。
- 注文ピラミッディングは方向ごとに 1 つのネットポジションへ制限されます。反転では、StockSharp のネッティングされたポジションモデルを反映し、まず反対エクスポージャーを閉じます。
- マルチタイムフレームデータは、オンデマンドの一時的なインジケーター要求ではなく、追加のローソク足購読で提供されます。

## 取引ロジック
### ロングエントリー
1. EMA 整列: 主要時間枠で 8 > 13 > 21 > 55。
2. 上位時間枠 momentum: 14 期間 Momentum の中立 100 レベルからの絶対偏差が、直近 3 本の上位時間枠ローソク足の少なくとも 1 本で、設定された買いしきい値を上回ります。
3. 月次 MACD フィルター: MACD メインラインがシグナルラインより上にあります。
4. ポジションフィルター: 現在のネットポジションはフラットまたはショートで、設定された最大数量を下回っている必要があります。

### ショートエントリー
1. EMA 整列: 8 < 13 < 21 < 55。
2. 直近 3 本の上位時間枠ローソク足の少なくとも 1 本で、momentum 偏差が売りしきい値を上回ります。
3. MACD メインラインがシグナルラインより下にあります。
4. ネットエクスポージャーはフラットまたはロングで、`MaxPositions` 制限内である必要があります。

### エグジットロジック
- `StartProtection` は、pip 距離で表された保護 stop-loss と take-profit 注文を配置します。この移植版では追加の trailing または break-even ロジックは適用されません。
- 反転シグナルは反対方向の成行注文サイズを送信し、まず既存ポジションを相殺してから新しいエクスポージャーを確立します。

## マルチタイムフレーム対応
momentum インジケーターに使用する上位時間枠は、元の係数テーブルを反映します。

| 主要時間枠 | Momentum時間枠 |
| --- | --- |
| 1 分 | 15 分 |
| 5 分 | 30 分 |
| 15 分 | 1 時間 |
| 30 分 | 4 時間 |
| 1 時間 | 1 日 |
| 4 時間 | 1 週間 |
| 1 日 | 1 か月 |
| 1 週間 | 1 か月 |
| 1 か月 | 1 か月 |

MACD 確認は常に 30 日の月次近似で実行されます。

## パラメーター
| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `TradeVolume` | ロット単位の注文サイズ。 | 0.1 |
| `StopLossPips` | pips 単位の stop-loss 距離。 | 20 |
| `TakeProfitPips` | pips 単位の take-profit 距離。 | 50 |
| `MomentumBuyThreshold` | ロング取引に必要な 100 からの最小絶対 momentum 偏差。 | 0.3 |
| `MomentumSellThreshold` | ショート取引に必要な 100 からの最小絶対 momentum 偏差。 | 0.3 |
| `MaxPositions` | `TradeVolume` の倍数として表される方向ごとの最大ネット数量。 | 1 |
| `CandleType` | EMA 計算の主要時間枠。 | 1 時間足 |

## 使用上の注意
1. 戦略をシンボルに接続し、`CandleType` で適切な時間枠を選択します。
2. データソースが主要時間枠と派生した上位時間枠 (momentum と月次) の両方を提供できることを確認してください。
3. 商品の tick サイズに合わせて pip ベースのリスクパラメーターを調整します。ヘルパーは `Security.PriceStep` を使って pips を商品ステップへ変換します。
4. バックテストと最適化では、提供されたパラメーター範囲を使って momentum しきい値と stop 距離を微調整できます。
