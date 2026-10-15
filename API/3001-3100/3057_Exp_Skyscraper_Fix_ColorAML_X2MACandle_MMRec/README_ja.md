# Exp Skyscraper Fix + ColorAML + X2MA Candle MMRec戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTraderエキスパート **Exp_Skyscraper_Fix_ColorAML_X2MACandle_MMRec** のC#変換。
- 3つの独立したカラーベースのフィルター（Skyscraper Fixチャンネル、ColorAML適応レベル、X2MACandle二重平滑化ローソク足）を組み合わせます。
- 各フィルターは同じシンボルを共有しながら独自にトレードを開いたり閉じたりでき、協調的なトレンドフォローと迅速な反転を可能にします。
- 簡略化されたマネー管理モジュールを含みます：ある方向の最近のトレードが繰り返し損失を出すと、モジュールは削減ボリューム（`SmallMM`）に切り替わります。

## 戦略ロジック
### Skyscraper Fixブロック
1. ATRレンジと選択した価格ソース（high/lowまたはclose）を分析してSkyscraper Fixトレーリングチャンネルを構築します。
2. チャンネルの色が強気になると、ブロックは：
   - 未決のショートポジションをクローズします（`Skyscraper Close Shorts`が有効な場合）；
   - 設定されたシグナル遅延後に新しいロングポジションを開くかもしれません（`Skyscraper Buy`が有効な場合）。
3. 色が弱気になると、ロジックはショートトレードのためのステップを反転させます。
4. High/lowエンベロープ、ATR乗数（`Kv`）、パーセントオフセットは元のインジケーターの動作を再現します。

### ColorAMLブロック
1. 2つの連続するフラクタルウィンドウのレンジを測定し、合成価格を平滑化することで適応市場レベル（AML）を計算します。
2. インジケーターは3つの色を出力します：`2`（強気）、`0`（弱気）、`1`（ニュートラル）。ニュートラルなローソク足はアクションをトリガーしません。
3. 強気の色は（許可されている場合）ショートをクローズし、前の検査されたローソク足で色が強気から変わっていた場合にロングを開くかもしれません。
4. 弱気の色はショートトレードの対称的なアクションを実行します。

### X2MACandleブロック
1. 各OHLC要素（オープン、ハイ、ロー、クローズ）に設定可能な2つの移動平均をカスケードして合成ローソク足を構築します。
2. 色は平滑化されたローソク足の本体によって決まります：クローズ > オープンの場合は強気、クローズ < オープンの場合は弱気、そうでなければニュートラル。
3. 小さなギャップ閾値（価格ステップ単位）は非常に小さいローソク足の本体を平坦化して急速な色の変動を避けます。
4. 強気の色はショートをクローズしてロングを開くことができます；弱気の色はその逆を実行します。

### マネー管理
1. 各ブロックはロングとショートの方向について独自のトレード履歴を独立して管理します。
2. トレードがクローズした後、モジュールは損失で終わったかどうかを記録します。
3. ある方向の最後の`Loss Trigger`トレードがすべて損失だった場合、そのブロックからの次の注文は削減ボリューム（`SmallMM`）に切り替わります。
4. 利益のあるまたはニュートラルなトレードが損失の連続を破ると、モジュールは自動的に通常のボリューム（`MM`）に戻ります。

## パラメーター
| セクション | 名前 | 説明 | デフォルト |
| --- | --- | --- | --- |
| Skyscraper | `Skyscraper Candle` | Skyscraper Fixインジケーターのローソク足をサンプリングするタイムフレーム。 | 4h |
| Skyscraper | `Skyscraper Length` | ATR平均ウィンドウ（ローソク足の数）。 | 10 |
| Skyscraper | `Skyscraper Kv` | ATRステップに適用される感度乗数。 | 0.9 |
| Skyscraper | `Skyscraper Percentage` | 中間線に追加/削除される追加パーセンテージ。 | 0 |
| Skyscraper | `Skyscraper Mode` | エンベロープに使用される価格ソース（High/LowまたはClose）。 | High/Low |
| Skyscraper | `Skyscraper Signal Bar` | 色に対して行動する前に待つ既にクローズ済みのローソク足の数。 | 1 |
| Skyscraper | `Skyscraper Buy` / `Skyscraper Sell` | ロング/ショートトレードのオープンを許可。 | true |
| Skyscraper | `Skyscraper Close Long` / `Skyscraper Close Short` | このブロックがロング/ショートトレードを終了することを許可。 | true |
| Skyscraper | `Skyscraper Normal Volume` | 基本注文ボリューム（EAでの`MM`）。 | 0.1 |
| Skyscraper | `Skyscraper Reduced Volume` | 損失ストリーク後に使用する削減注文ボリューム（`SmallMM`）。 | 0.01 |
| Skyscraper | `Skyscraper Buy Loss Trigger` / `Skyscraper Sell Loss Trigger` | 削減ボリュームに切り替える連続損失の数。 | 2 |
| ColorAML | `ColorAML Candle` | ColorAMLインジケーターが使用するローソク足タイプ。 | 4h |
| ColorAML | `ColorAML Fractal` | レンジ計算に使用するフラクタルウィンドウ（バー単位）。 | 6 |
| ColorAML | `ColorAML Lag` | 適応平滑を制御するラグパラメーター。 | 7 |
| ColorAML | `ColorAML Signal Bar` | 色を評価する前に適用されるローソク足オフセット。 | 1 |
| ColorAML | `ColorAML Buy` / `ColorAML Sell` | ColorAMLによって生成されるロング/ショートエントリーを有効化。 | true |
| ColorAML | `ColorAML Close Long` / `ColorAML Close Short` | ColorAMLがロング/ショートポジションをクローズすることを許可。 | true |
| ColorAML | `ColorAML Normal Volume` / `ColorAML Reduced Volume` | このブロックの基本ボリュームと削減ボリューム。 | 0.1 / 0.01 |
| ColorAML | `ColorAML Buy Loss Trigger` / `ColorAML Sell Loss Trigger` | 削減ボリュームを有効にする連続損失。 | 2 |
| X2MA | `X2MA Candle` | X2MACandle再構築に使用するタイムフレーム。 | 4h |
| X2MA | `First Method` / `Second Method` | 第1および第2移動平均の平滑化方法。 | SMA / JJMA |
| X2MA | `First Length` / `Second Length` | 2つの平滑化ステージの期間。 | 12 / 5 |
| X2MA | `First Phase` / `Second Phase` | Jurik移動平均が使用する互換性フェーズ。 | 15 |
| X2MA | `Gap Points` | 小さなローソク足の本体を平坦化するギャップ閾値（価格ステップ単位）。 | 10 |
| X2MA | `X2MA Signal Bar` | 色に反応する前に適用されるローソク足オフセット。 | 1 |
| X2MA | `X2MA Buy` / `X2MA Sell` | X2MACandleブロックからのロング/ショートトレードのオープンを許可。 | true |
| X2MA | `X2MA Close Long` / `X2MA Close Short` | ブロックがロング/ショートポジションを終了することを許可。 | true |
| X2MA | `X2MA Normal Volume` / `X2MA Reduced Volume` | X2MACandleトレードの基本ボリュームと削減ボリューム。 | 0.1 / 0.01 |
| X2MA | `X2MA Buy Loss Trigger` / `X2MA Sell Loss Trigger` | 削減ボリュームに切り替える前の連続損失の数。 | 2 |

## 使用上のヒント
1. 市場のボラティリティに合わせてローソク足タイプを調整します（例：イントラデイ取引には1h、スイング取引には4h）。
2. 3つのモジュールは独立して調整できます — 1つのブロックを無効にしても他のブロックはアクティブのまま残ります。
3. マネー管理の閾値は意図的に保守的です。計器が強くトレンドしていてベースボリュームをより長く維持したい場合は、トリガーを増やしてください。
4. 戦略は完了したローソク足に依存するため、設定されたタイムフレームに一致するローソク足データを常に供給してください。
