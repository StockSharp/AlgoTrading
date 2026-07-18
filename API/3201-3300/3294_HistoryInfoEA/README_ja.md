# HistoryInfoEaStrategy戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**HistoryInfoEaStrategy** は、StockSharp 上で MT4 の "HistoryInfo" ユーティリティを再現します。MetaTrader チャートにテキストを描画する代わりに、戦略は `OnNewMyTrade` ストリームを監視し、選択したフィルターに一致する取引の統計を集計します。集計値は `LastSnapshot` プロパティを通じて公開され、戦略ログにも反映されるため、GUI や自動化スクリプトは任意の形式で要約を表示できます。

戦略は自分の注文を登録しません。他の自動または手動戦略がブローカーへ注文を送信している間に、その横で実行されるように設計されています。フィルターを満たす約定済み取引はすべて合計に寄与します。

## パラメーター
| パラメーター | 説明 |
|-----------|------|
| `FilterType` | 取引をどのように照合するかを決める選択モード。対応値: `CountByUserOrderId`, `CountByComment`, `CountBySecurity`。 |
| `MagicNumber` | 期待される `Order.UserOrderId`。`FilterType` が `CountByUserOrderId` の場合のみ使用します。このフィルターを無効にするには空にします。 |
| `OrderComment` | `Order.Comment` と一致する必要があるプレフィックス。`CountByComment` モードでのみ関連します。デフォルト値 (`\"OrdersComment\"`) は MT4 スクリプトのプレースホルダーを模倣しており、置き換えるまでは通常どの注文にも一致しません。 |
| `SecurityId` | `FilterType` が `CountBySecurity` の場合に一致する必要がある商品の識別子 (`Security.Id`)。デフォルト (`\"OrdersSymbol\"`) はプレースホルダーです。 |

## 集計指標
`LastSnapshot` は一致する各取引の後に更新されます。内容は次のとおりです。

- `FirstTrade` / `LastTrade` - 処理済み取引の最初と最後のタイムスタンプ。
- `TotalVolume` - 取引の数量単位 (ロット、契約など) で表された累積約定数量。
- `TotalProfit` - `MyTrade.PnL` から報告済み手数料を差し引いた合計で、口座通貨での実現利益を表します。
- `TotalPips` - `Security.PriceStep`、`Security.StepPrice`、MT4 風の桁処理 (5/3 桁ではポイントを 10 倍) を使って pips へ変換した利益。
- `TradeCount` - フィルターを通過した取引数。

同じ情報は、すばやく確認できるように MT4 の `Comment()` 出力を模倣して、戦略ログへ 1 行で出力されます。

## 使用方法
1. 他の戦略が注文送信に使うものと同じポートフォリオおよび銘柄に戦略を接続します。
2. 希望する `FilterType` を選び、関連パラメーター (magic number、コメントプレフィックス、または銘柄識別子) を入力します。
3. 戦略を開始します。条件に一致する最初の取引が約定すると、合計が `LastSnapshot` とログで利用可能になります。
4. カウンターは、戦略の再起動または手動リセットごとに自動的にリセットされます。

> **注意:** pip 合計を計算するには、戦略は正しい商品メタデータに依存します。`Security.PriceStep` と `Security.StepPrice` が board 定義で設定されていることを確認してください。どちらかが欠けている場合、利益値は蓄積され続けますが、pip カウンターはゼロのままです。

## 変換メモ
- MT4 コードは各 tick で `OrdersHistoryTotal()` を反復していました。StockSharp では、戦略はリアルタイムの `MyTrade` 通知に反応するため、ポーリングはなく、fill が到着すると即座に計算が更新されます。
- MT4 は利益を `OrderProfit + OrderCommission + OrderSwap` として保存していました。StockSharp は `MyTrade.PnL` 経由で実現利益を、手数料を別に提供します。swap は通常 PnL に含まれています。この移植版は元のレポートとの一貫性を保つため、`PnL` から手数料を差し引きます。
- 文字列プレースホルダー (`\"OrdersComment\"`, `\"OrdersSymbol\"`) は元のデフォルトに似せるため保持されています。一致を期待する場合は、戦略開始前に実際の値へ置き換えてください。
- MT4 の視覚的なチャート出力は、構造化データ (`LastSnapshot`) とログ行に置き換えられ、インテグレーターが表示方法を決められます。
- 戦略は意図的に新しい注文を作成しないため、第三者の取引ストリームを干渉せず分析する read-only モードで起動できます。

## 拡張アイデア
- `LastSnapshot` 更新を購読し、情報をダッシュボードやテレメトリーコレクターへ転送します。
- コネクターが関連メタデータを提供する場合、ポートフォリオやカスタム戦略タグなどの追加フィルターでクラスを拡張します。
- 戦略を定期タイマーと組み合わせ、履歴要約を CSV/JSON レポートへエクスポートします。
