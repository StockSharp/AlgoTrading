# iTrade戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader エキスパートアドバイザー **iTrade** から変換された手動売りマネージャーです。元 EA のチャートボタンワークフローを再現します。ユーザーが売りを要求するたびに、martingale ポジションが開かれます。その後、戦略はすべてのショート取引の含み益を監視し、事前定義された利益目標に達すると、最も利益のある ticket と最も利益の少ない ticket を清算します。

## 中核ロジック

- 注文は明示的なユーザー要求でのみ開かれます。MetaTrader のボタン押下をシミュレートするには `QueueSellRequest()` を呼び出します。
- 最初のポジションは設定された **Initial Volume** を使用します。損失取引の後は毎回、次の注文サイズが **Martingale Multiplier** で乗算されます。利益取引はシーケンスを基本数量へリセットします。
- 含み益は現在の最良 ask 価格を使って測定されます。オープン取引あたりの平均利益が **Average Profit Target** に達すると、戦略はアクティブバッチから最も利益のある取引と最も利益の少ない取引を閉じます (最大 **Base Trade Count** 取引)。
- **Base Trade Count** を超えるポジションが開いている場合、2 つの取引を閉じる前に、より厳しい **Extended Profit Target** が適用されます。
- 利益計算は銘柄の `PriceStep` と `StepPrice` 値に依存します。これらがない場合、戦略は起動時に例外を投げます。

## パラメーター

| 名前 | 説明 |
| ---- | ---- |
| `InitialVolume` | 最初の martingale 注文に使用する基本ロットサイズ。 |
| `MartingaleMultiplier` | 各損失取引後に適用される乗数。 |
| `AverageProfitTarget` | 最初のバッチ内の取引を閉じるために必要な平均含み益 (通貨)。 |
| `ExtendedAverageProfitTarget` | 基本バッチを超えてアクティブな場合の平均含み益しきい値。 |
| `BaseTradeCount` | 初期バッチの一部とみなされる取引数。 |
| `ControlInterval` | 内部チェックの頻度 (タイマー間隔)。 |

## 使用上の注意

1. 戦略を開始する前に、`Security`、`Portfolio`、必要なパラメーターを設定します。
2. 新しい売りを開く必要があるたびに `QueueSellRequest()` を呼び出します。戦略は martingale ルールに従って注文サイズを決め、成行売りを送信します。
3. アルゴリズムは、元の martingale 動作を再現するため、クローズ済み取引結果の履歴 (最大 200 件) を保存します。
4. 決済注文は、対象取引の正確な数量で成行買いとして送信されます。

## MetaTrader版との違い

- MetaTrader 版はチャートボタンに依存していました。ここではユーザーが `QueueSellRequest()` 経由でプログラム的に売りをトリガーします。
- 注文実行は StockSharp の成行注文で処理されます。部分約定は戦略によって自動的に集約されます。
- 利益しきい値は `StepPrice` を使った decimal 通貨値で動作しますが、元 EA は MetaTrader の ticket 利益関数を使用していました。
