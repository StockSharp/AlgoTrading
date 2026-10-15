# Pending Stop Grid戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Pending Stop Grid戦略**は、MetaTrader 4 エキスパートアドバイザー `new.mq4` を直接変換したものです。戦略は 2 つの対称的な保留注文の階段を維持します。

- 現在の ask 価格より上の buy stop 注文列。
- 現在の bid 価格より下の sell stop 注文列。

追加の各レベルは、階段内の位置に比例して注文距離と取引数量の両方を増やします。stop-loss と take-profit の目標は各注文に個別に割り当てられます。

## 取引ロジック
1. 戦略は Level 1 データを購読し、最新の最良 bid と ask 価格を継続的に追跡します。
2. 市場データと取引許可が利用可能になると、銘柄の価格ステップを使って pip サイズを計算します (5 桁および 3 桁シンボルは標準 pip 値へ自動正規化)。
3. 注文を出す前に、設定された基本数量が商品の最小および最大数量制約を満たしていることを検証します。
4. 1 から `NumberOfTrades` までの各インデックス `i` について:
   - 注文数量は `BaseVolume * i` として計算され、許可された最も近いステップに丸められます。
   - 任意の stop-loss と take-profit オフセットを付けて、`Ask + DistancePips * i * pipSize` に buy stop を配置します。
   - 反転した stop-loss と take-profit オフセットを付けて、`Bid - DistancePips * i * pipSize` に sell stop を配置します。
5. 注文が約定、キャンセル、または拒否された場合、階段内の対応スロットをクリアし、市場データが許すとすぐに新しい保留注文で補充します。
6. プラットフォームのリスク制御を有効にするため、起動時に組み込みの `StartProtection()` が呼び出されます。

## パラメーター
| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `BaseVolume` | 最初の保留注文の数量。後続の各注文は、この基本値に自身のインデックスを掛けます。 | `0.1` |
| `NumberOfTrades` | 同時に維持される buy stop と sell stop 注文の数。 | `10` |
| `DistancePips` | 市場価格と各保留注文レベルの間の距離 (pips)。 | `10` |
| `StopLossPips` | 各注文に割り当てられる stop-loss 距離。stop-loss 配置を無効にするにはゼロに設定します。 | `10` |
| `TakeProfitPips` | 各注文に割り当てられる take-profit 距離。take-profit 配置を無効にするにはゼロに設定します。 | `10` |

すべてのパラメーターは最適化可能な戦略パラメーターとして公開され、該当する場合には負値またはゼロ値を避けるために検証されます。

## 追加の注意事項
- 数量は最も近い許容ステップへ丸められ、取引所定義の最小および最大境界内に制限されます。
- 価格は `Security.ShrinkPrice` で正規化され、商品の tick サイズを尊重します。
- 戦略は履歴状態を保持しません。銘柄がリセットされるか取引許可が変わるたびに、階段全体を再構築します。
- ロジックは手動インジケーターバッファーを避け、StockSharp 高レベル API バインディングを使用し、プロジェクト全体の変換ガイドラインに従います。
