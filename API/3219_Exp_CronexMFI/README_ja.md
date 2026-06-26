# Exp Cronex MFI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は**Exp_CronexMFI**エキスパートアドバイザーを複製します。マネーフローインデックス（MFI）を2回平滑化し、結果として得られるラインのクロスオーバーに**逆張り**でトレードします。ポートは元の逆張り哲学を維持しながら、すべての設定をStockSharp戦略パラメーターとして公開します。

## 仕組み
1. 選択されたローソク足シリーズにサブスクライブする（デフォルトは4時間足）。
2. 設定された期間でマネーフローインデックスを計算する。
3. 選択した平滑化方法を2回適用する：最初のパスがファストCronexラインを生成し、2回目のパスがファストラインを再び平滑化してスローラインを構築する。
4. 調整可能な遅延（`SignalShift`）でファストとスロー値の履歴ペアを保存する。
5. ファストラインがスローラインを**下向き**にクロスしたとき、ショートを閉じ（許可されている場合）、ロングポジションを開く/拡大する。ファストラインが**上向き**にクロスしたとき、ロングを閉じてショートポジションを開く/拡大する。
6. 注文は戦略の`Volume`で送信され、ロング側とショート側で独立して無効化できます。

戦略はMetaTrader実装のタイミングに合わせるため、完成したローソク足のみを評価します。

## パラメーター
| 名前 | 型 | デフォルト | 説明 |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | マネーフローインデックスの長さ。 |
| `FastPeriod` | `int` | `14` | 第1平滑化ステージ（ファストCronexライン）の期間。 |
| `SlowPeriod` | `int` | `25` | 第2平滑化ステージ（スローCronexライン）の期間。 |
| `SignalShift` | `int` | `1` | シグナル処理を遅延させる完成ローソク足の数。MQLの`SignalBar`動作を再現。 |
| `Smoothing` | `SmoothingMethod` | `Simple` | 両方の平滑化ステージに使用する移動平均アルゴリズム。 |
| `EnableLongEntries` | `bool` | `true` | ロングポジションを開くまたは追加する成行注文を有効化。 |
| `EnableShortEntries` | `bool` | `true` | ショートポジションを開くまたは追加する成行注文を有効化。 |
| `EnableLongExits` | `bool` | `true` | シグナルが既存のロング露出を閉じることを許可。 |
| `EnableShortExits` | `bool` | `true` | シグナルが既存のショート露出を閉じることを許可。 |
| `CandleType` | `DataType` | `TimeFrame(4h)` | インジケーター計算に使用するローソク足シリーズ。 |
| `Volume` | `decimal` | `1` | 新しいポジションを開く際に使用する注文サイズ。 |

## 平滑化オプション
元のMQLインジケーターはいくつかの独自平滑化モードを提供します。StockSharpポートはそれらを組み込み移動平均にマッピングします：

| MLQコンセプト | `SmoothingMethod`値 | 備考 |
| --- | --- | --- |
| SMA | `Simple` | 単純移動平均。 |
| EMA | `Exponential` | 指数移動平均。 |
| SMMA | `Smoothed` | 平滑化移動平均（Wilder）。 |
| LWMA | `Weighted` | 線形加重移動平均。 |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | 適応型平滑化に最も近い近似を選択。 |

## MQLバージョンとの違い
- MQLからのティック/実際のボリューム選択は利用できない；StockSharpのローソク足は集計ボリュームデータを提供する。
- トレード管理は成行注文のみに依存する。次のバーまで実行を遅延させた元の資金管理ヘルパーは`SignalShift`を通じてエミュレートされる。
- ストップロスとテイクプロフィットの配置は外部で設定する必要がある（例：リスクルールまたは保護モジュール経由）。

## 使用上の注意
- インストゥルメントの流動性に合ったローソク足シリーズを選ぶ；デフォルトの4時間インターバルはソースEAを反映する。
- 追加ローソク足でクロスオーバーを確認したい場合は`SignalShift`を調整する。
- 損失を抑えるためにリスク管理ルール（例：`StartProtection`）と戦略を組み合わせる。
