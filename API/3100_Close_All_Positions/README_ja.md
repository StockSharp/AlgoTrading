# 3100 すべてのポジションを決済する
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MQL5ユーティリティ**Close all positions**をStockSharpの高レベル戦略に変換します。
- 設定された時間軸の完了ローソク足を監視し、割り当てられたポートフォリオ内のすべてのオープンポジションの浮動利益を累積します。
- 浮動利益が閾値以上になると、ブックが完全に閉じるまで戦略（子戦略を含む）が管理するすべての証券をフラットにするために成行注文が送られます。
- `_closeAllRequested`フラグはMQL変数`m_close_all`を反映し、ポジションが残らなくなるまでエグジット注文が継続して発行されます。

## パラメーター
| 名前 | 型 | デフォルト | 説明 |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | 戦略がすべてのオープンポジションをフラットにする前に必要な浮動利益（口座通貨）。EAの`InpProfit`を反映します。 |
| `CandleType` | `DataType` | `1m`時間軸 | "新しいバー"の瞬間を定義するローソク足シリーズ。利益チェックはローソク足が完了したときにのみ実行され、元の`PrevBars`ロジックをエミュレートします。 |

## トレードロジック
1. 戦略は`CandleType`のローソク足をサブスクライブし、EAが新しいバーでのみ利益を評価したのと同様に、完了したバーのみを処理します。
2. 各完了バーで、ヘルパー`CalculateTotalProfit`は`Portfolio.CurrentProfit`（コミッションとスワップを含む浮動PnL）を取得します。アダプターがこの値を提供できない場合、個々のポジション`PnL`値の合計にフォールバックします。
3. 計算された浮動利益が`ProfitThreshold`を下回る場合、何も起こりません。
4. 利益が閾値に達するとすぐに、`_closeAllRequested`が`true`に設定され、`CloseAllPositions()`がすぐに実行されます。
5. `CloseAllPositions()`はポートフォリオまたはネストされた戦略でエクスポージャーを持つすべての証券を収集し、現在のボリュームの反対方向に成行注文を送ります（ロング→売り、ショート→買い）。
6. `_closeAllRequested`フラグは`HasAnyOpenPosition()`がポートフォリオがフラットであることを検出するまで設定されたままになり、`m_close_all`がすべてのチケットが閉じられるまで真のままだったMQLの動作に一致します。

## 追加の注意事項
- C#実装のみが提供されます；Pythonフォルダーはタスク要件に従って意図的に空のままにされています。
- 元のスクリプトが成行ポジションのみを閉じたため、戦略は保留注文をキャンセルしません。
- 必要に応じてDesignerオプティマイザーを通じて代替の利益目標を探るために、`ProfitThreshold`に`SetOptimize`を使用します。

## ファイル
- `CS/CloseAllPositionsStrategy.cs`
