# 注文クローズ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このユーティリティ戦略は、ユーザー定義のフィルターに従って既存のポジションを即座にクローズし、保留中の注文をキャンセルします。添付された銘柄のみ、またはポートフォリオ内のすべての銘柄を対象に動作できます。オプションの時間ウィンドウと価格範囲の制限により、どの注文が影響を受けるかを正確に制御できます。

## 詳細

- **目的**: リスク管理と手動清算。
- **動作**:
  - 開始時に、戦略はオプションの時間ウィンドウを確認する。
  - 許可されている場合、フィルターに一致するポジションをクローズし注文をキャンセルする。
  - 処理後、戦略は自動的に停止する。
- **フィルター**:
  - `CloseAllSecurities` – 添付された銘柄のみではなく、ポートフォリオ内のすべての銘柄を対象にする。
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – 既存のロングまたはショートポジションをクローズする。
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – 保留中の買い注文または売り注文をキャンセルする。
  - `SpecificOrderId` – ゼロ以外の場合、指定されたトランザクションIDを持つ注文のみを対象にする。
  - `CloseOrdersWithinRange`、`CloseRangeHigh`、`CloseRangeLow` – エントリー価格範囲で制限する。
  - `EnableTimeControl`、`StartCloseTime`、`StopCloseTime` – 特定の時間ウィンドウ中のみ適用する。
- **デフォルト値**:
  - すべてのクローズオプションが有効。
  - `SpecificOrderId` = 0.
  - `CloseOrdersWithinRange` = false.
  - `CloseRangeHigh` = 0.
  - `CloseRangeLow` = 0.
  - `EnableTimeControl` = false.
  - `StartCloseTime` = 02:00.
  - `StopCloseTime` = 02:30.
- **注記**:
  - この戦略は新しいポジションを開かない。
  - 境界値がゼロまたは負の場合、価格範囲フィルターは無視される。
  - `CloseAllSecurities` が有効な場合、ポートフォリオ全体のポジションが処理される。
