# XFatlXSatlCloud Duplex 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
XFatlXSatlCloud Duplexは元のMQL5エキスパートアドバイザーから変換された双方向戦略です。高速なFATLデジタルフィルターと低速なSATLフィルターを組み合わせ、両方を設定可能な移動平均で滑らかにするXFatlXSatlCloudインジケーターのクロスオーバーを取引します。ロングとショートサイドに別々の設定を適用でき、異なる時間軸、平滑化方法、適用価格ソースが含まれます。

## トレードロジック
戦略は完成した足のみを評価します。2つの独立したサブスクリプションが並行して実行されます：1つはロングロジックを、もう1つはショートロジックを駆動します。各サブスクリプションはC#で実装されたXFatlXSatlCloudインジケーターを供給し、以下の動作を生成します：

- **ロングエントリー** – `LongSignalBar`で定義されたバーで高速ラインが低速ラインを上抜けするときに発動します。ショートポジションが開いている場合、まずクローズされます（`ShortAllowClose`が有効な場合のみ）。次に`LongVolume`枚のマーケット買い注文が送信され、エントリー価格がリスクチェック用に記録されます。
- **ロングエグジット** – シフトされたバーで高速ラインが低速ラインを下抜けするときに実行されます。オプションの価格ベースのストップロスとテイクプロフィットチェック（`LongStopLoss`、`LongTakeProfit`）は、足のレンジが定義されたオフセットに違反した場合、ポジションをより早くクローズすることができます。
- **ショートエントリー** – `ShortSignalBar`で定義されたバーで高速ラインが低速ラインを下抜けするときに発動します。`LongAllowClose`が有効な場合、既存のロングエクスポージャーが最初にフラット化されます。その後`ShortVolume`枚のマーケット売り注文が送信されます。
- **ショートエグジット** – シフトされたバーで高速ラインが低速ラインを上抜けするときに実行されます。オプションのリスクコントロール（`ShortStopLoss`、`ShortTakeProfit`）がバー内の極値を監視します。

すべてのインジケーター値は完成した足でのみ計算され、各決定が最終データに基づき元のMQL動作を反映することを保証します。

## リスク管理
戦略はロングとショートポジションのために最後のエントリー価格を別々に追跡します。ストップロスまたはテイクプロフィットオフセットが指定されており、現在の足が対応する閾値を超える場合、ポジションは即座にクローズされます（関連する`AllowClose`フラグに従います）。オフセットは取引されるインストゥルメントの絶対価格単位で測定されます。

## パラメーター
| グループ | 名前 | 説明 |
| --- | --- | --- |
| Trading | `LongVolume` | ロングエントリーの注文サイズ（ゼロより大きい）。 |
| Trading | `ShortVolume` | ショートエントリーの注文サイズ（ゼロより大きい）。 |
| Trading | `LongAllowOpen` | 新しいロングポジションの開設を有効/無効にします。 |
| Trading | `LongAllowClose` | ロングエグジットを有効/無効にします（ストップとクロスエグジットに必要）。 |
| Trading | `ShortAllowOpen` | 新しいショートポジションの開設を有効/無効にします。 |
| Trading | `ShortAllowClose` | ショートエグジットを有効/無効にします。 |
| Signals | `LongSignalBar` | ロングのクロスオーバーをチェックする際に振り返る完成したバーの数。 |
| Signals | `ShortSignalBar` | ショートのクロスオーバーをチェックする際に振り返る完成したバーの数。 |
| Data | `LongCandleType` | ロングインジケーターサブスクリプションに使用する足タイプ（時間軸）。 |
| Data | `ShortCandleType` | ショートインジケーターサブスクリプションに使用する足タイプ。 |
| Indicators | `LongMethod1` | ロングサイドのFATL出力に適用する平滑化方法。サポート値: SMA、EMA、SMMA、LWMA、Jurik、ZeroLag、Kaufman。 |
| Indicators | `LongLength1` | 高速ロングスムーザーの長さ。 |
| Indicators | `LongPhase1` | 高速スムーザーに転送されるフェーズパラメーター（互換性のために保持、概念的にJurikのみが使用）。 |
| Indicators | `LongMethod2` | ロングサイドのSATL出力に適用する平滑化方法（上記と同じサポートセット）。 |
| Indicators | `LongLength2` | 低速ロングスムーザーの長さ。 |
| Indicators | `LongPhase2` | 低速ロングスムーザーのフェーズパラメーター。 |
| Indicators | `LongAppliedPrice` | ロングインジケーターの構築に使用する適用価格（終値、始値、中値、典型値、加重値、単純、四分値、トレンドフォローまたはDemark）。 |
| Indicators | `ShortMethod1` | 高速ショートラインの平滑化方法。 |
| Indicators | `ShortLength1` | 高速ショートスムーザーの長さ。 |
| Indicators | `ShortPhase1` | 高速ショートスムーザーのフェーズパラメーター。 |
| Indicators | `ShortMethod2` | 低速ショートラインの平滑化方法。 |
| Indicators | `ShortLength2` | 低速ショートスムーザーの長さ。 |
| Indicators | `ShortPhase2` | 低速ショートスムーザーのフェーズパラメーター。 |
| Indicators | `ShortAppliedPrice` | ショートインジケーターの構築に使用する適用価格。 |
| Risk | `LongStopLoss` | ロングストップロスの絶対価格距離（0でチェックを無効化）。 |
| Risk | `LongTakeProfit` | ロングテイクプロフィットの絶対価格距離（0でチェックを無効化）。 |
| Risk | `ShortStopLoss` | ショートストップロスの絶対価格距離（0でチェックを無効化）。 |
| Risk | `ShortTakeProfit` | ショートテイクプロフィットの絶対価格距離（0でチェックを無効化）。 |

## 実装メモ
- XFatlXSatlCloudインジケーターはStockSharpの高レベルインジケーターとして実装されています。高速と低速のコンポーネントは元のFATL/SATLの有限インパルス応答係数を適用し、続いてユーザーが選択した平滑化インジケーターを適用することで生成されます。
- 一般的に利用可能なStockSharpの移動平均のみが公開されています（`Sma`、`Ema`、`Smma`、`Lwma`、`Jurik`、`ZeroLag`、`Kaufman`）。他のMQL平滑化ファミリー（ParabolicやT3など）は含まれていません。
- `LongSignalBar`と`ShortSignalBar`は元の`SignalBar`パラメーターを模倣します。1の値はクロスオーバー検出時に「前の完成したバーを使用する」ことを意味します。
- ストップロスとテイクプロフィットオフセットは絶対価格距離を期待します。記録されたエントリー価格に対する足の高値/安値を使用して適用され、ブローカー固有のポイント値に依存しません。
