# Vortex Indicator MMRec Duplex戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 5エキスパート **Exp_VortexIndicator_MMRec_Duplex.mq5**（MQL ID 23180）から変換。
- 2つの独立したVortexインジケーターストリームを維持：1つはロングトレード専用、もう1つはショートトレード専用。各ストリームは独自の時間軸、期間、バーシフトを持つため、強気と弱気のロジックを個別に調整できます。
- オリジナルEAの「MMRec」マネーマネジメント・リカバリーモジュールを複製。この戦略は方向ごとの最新トレード結果を追跡し、設定可能な損失回数の後に一時的に縮小した注文サイズに切り替えます。

## シグナルロジック
1. 各ストリームに設定されたローソクタイプを購読し、Vortexインジケーター（`VI+` と `VI-`）を計算します。
2. **ロングエントリー：** 前のバーが `VI+` ≤ `VI-` で、現在のバーが `VI+` > `VI-` で終値を付けた場合（強気クロスオーバー）。`AllowLongEntries` が有効な場合のみエントリー可能。
3. **ロング決済：** 評価バーで `VI-` が `VI+` を上回った場合（`AllowLongExits` が有効な場合）。
4. **ショートエントリー：** 前のバーが `VI+` ≥ `VI-` で、現在のバーが `VI+` < `VI-` で終値を付けた場合（弱気クロスオーバー）、`AllowShortEntries` で制御。
5. **ショート決済：** 評価バーで `VI+` が `VI-` を上回って戻った場合、`AllowShortExits` で制御。
6. 各方向は価格ステップで測定された独自のストップロスとテイクプロフィットレベルを保持。どちらかのレベルに達すると即座にポジションを決済し、リカバリーカウンターに結果を登録します。

## マネーマネジメント・リカバリー
- オリジナルEAは過去のトレードのスライディングウィンドウを検査し、次の注文が通常または縮小ボリュームを使用するかを決定します。このポートは同じ動作を反映します。
- ロングトレードの場合、キューは最大 `LongTotalTrigger` 件の最新PnL結果を保存。少なくとも `LongLossTrigger` 件が損失トレードであれば、次のロングエントリーは `LongSmallMoneyManagement` を使用；それ以外は `LongMoneyManagement` を使用。
- ショートトレードは `ShortTotalTrigger`、`ShortLossTrigger`、`ShortSmallMoneyManagement`、`ShortMoneyManagement` で同じロジックを繰り返します。
- トリガー値がゼロの場合、キューはクリアされ基本ボリュームが常に使用されます。

## マージンモード
`MarginModeOption` はマネーマネジメント値が実行可能なボリュームに変換される方法を記述します：
- **FreeMargin (0):** 値を資本の一部として扱います（オリジナルの「フリーマージン」モードの近似）。
- **Balance (1):** このポートでは `FreeMargin` と同一；現在のポートフォリオ値を使用。
- **LossFreeMargin (2):** 設定されたストップロス距離を使用して資本の一部をリスクにさらします。ストップ距離がゼロの場合は価格ベースのサイジングにフォールバック。
- **LossBalance (3):** この実装では `LossFreeMargin` と同一。
- **Lot (4):** 値を直接注文ボリュームとして解釈。

計算された全サイズは、無効な送信を避けるため、銘柄のボリュームステップおよびボリューム最小・最大制約を使用して正規化されます。

## パラメーター
| パラメーター | デフォルト | 説明 |
| --- | --- | --- |
| `LongCandleType` | H4 | ロングサイドVortexインジケーターに使用する時間軸。 |
| `ShortCandleType` | H4 | ショートサイドVortexインジケーターに使用する時間軸。 |
| `LongLength` | 14 | ロングシグナル用Vortexインジケーターの期間。 |
| `ShortLength` | 14 | ショートシグナル用Vortexインジケーターの期間。 |
| `LongSignalBar` | 1 | ロングクロスオーバーで評価する終値バーオフセット（0 = 最新の終値バー）。 |
| `ShortSignalBar` | 1 | ショートクロスオーバーで評価する終値バーオフセット。 |
| `AllowLongEntries` | true | 強気クロスオーバー時のロングエントリーを有効化。 |
| `AllowLongExits` | true | `VI-` が `VI+` を支配する場合のロングポジション決済を有効化。 |
| `AllowShortEntries` | true | 弱気クロスオーバー時のショートエントリーを有効化。 |
| `AllowShortExits` | true | `VI+` が `VI-` を支配する場合のショートポジション決済を有効化。 |
| `LongTotalTrigger` | 5 | リカバリーカウンターが検査する直近ロングトレード数。 |
| `LongLossTrigger` | 3 | 縮小ロングボリュームに切り替わる前に必要な損失ロングトレード数。 |
| `LongMoneyManagement` | 0.1 | ロングトレードの基本マネーマネジメント値。 |
| `LongSmallMoneyManagement` | 0.01 | ロング損失連続後の縮小マネーマネジメント値。 |
| `LongMarginMode` | Lot | ロングマネーマネジメント値の解釈（上記モード参照）。 |
| `LongStopLossSteps` | 1000 | 価格ステップで表したロングエントリー以下の保護距離。 |
| `LongTakeProfitSteps` | 2000 | 価格ステップで表したロングエントリー以上のテイクプロフィット距離。 |
| `LongSlippageSteps` | 10 | ロング注文の情報スリッページ許容値（サイジングには使用されない）。 |
| `ShortTotalTrigger` | 5 | リカバリーカウンターが検査する直近ショートトレード数。 |
| `ShortLossTrigger` | 3 | 縮小ショートボリュームに切り替わる前に必要な損失ショートトレード数。 |
| `ShortMoneyManagement` | 0.1 | ショートトレードの基本マネーマネジメント値。 |
| `ShortSmallMoneyManagement` | 0.01 | ショート損失連続後の縮小マネーマネジメント値。 |
| `ShortMarginMode` | Lot | ショートマネーマネジメント値の解釈。 |
| `ShortStopLossSteps` | 1000 | 価格ステップで表したショートエントリー以上の保護距離。 |
| `ShortTakeProfitSteps` | 2000 | 価格ステップで表したショートエントリー以下のテイクプロフィット距離。 |
| `ShortSlippageSteps` | 10 | ショート注文の情報スリッページ許容値。 |

## 実装上の注記
- StockSharpの高レベルAPIで完全に構築。ローソク購読は `Bind` を通じてVortexインジケーターを駆動し、決定を下す前に終値バーを提供します。
- トレードリカバリーロジックは方向ごとの利益シリーズをキューに保存し、MetaTraderの `BuyTradeMMRecounterS` / `SellTradeMMRecounterS` 関数を反映します。
- ストップロスとテイクプロフィットレベルは価格単位（銘柄価格ステップ × 設定ステップ数）で再計算され、各着信ローソクで適用されます。
- 注文ボリュームは無効な送信を避けるため、銘柄の `VolumeStep`、`MinVolume`、`MaxVolume` 制約を通じて正規化されます。
- スリッページパラメーターはドキュメント目的で保持されていますが、StockSharpの注文ハンドラーでは直接使用されません。
