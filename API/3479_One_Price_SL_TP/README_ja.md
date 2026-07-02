# ワンプライスストップロス/テイクプロフィット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このユーティリティ戦略は、StockSharp 内で MetaTrader スクリプト「One Price SL TP」を複製します。 Instead of opening trades, the algorithm watches the current position on the configured instrument and makes sure that both protective orders are aligned with a single target price specified by the user.

パラメータ **`ZenPrice`** がゼロより大きい場合、戦略はそれを実際の入札/売値と比較します。

- **ロング** ポジションの場合: `ZenPrice` が売り値より高い場合、その価格で利食い指値注文が出されます。 `ZenPrice` が入札よりも低い場合、代わりに逆指値注文が登録されます。
- **ショート** ポジションの場合: `ZenPrice` が入札よりも低い場合、利食い指値注文になります。 `ZenPrice` がアスクよりも高い場合、それはストップロスのストップ注文になります。

価格がビッドとアスクの間にある場合は何も送信されないため、以前の保護命令はそのまま残ります。ポジションがクローズされるかパラメータがゼロにリセットされるとすぐに、すべての保護注文は自動的にキャンセルされます。

## 仕組み

1. レベル 1 データを購読して、方向チェックに必要な最新の買値/売値を受信します。
2. 現在の戦略ポジションの量と方向を追跡します。ポジションは手動または他の戦略によって作成されると想定されます。
3. On each quote, position or personal trade update, recalculates which side of the market the `ZenPrice` belongs to and builds the corresponding protective order type.
4. Normalises the requested price using the instrument price step and rounds the order volume to exchange limits before sending anything to the trading connector.
5. Uses `ReRegisterOrder` to modify already active protective orders instead of cancelling them, matching the behaviour of MetaTrader's in-place modification.

## パラメータ

- **`ZenPrice`** – ストップロスまたはテイクプロフィットレベルとして使用される絶対価格。自動化を無効にするには、値を `0` に設定します。デフォルト: `0`。

## 実践メモ

- この戦略ではエントリーオーダーは決して送信されません。裁量取引ターミナルや他の自動戦略と並行して始めるのが安全です。
- 保護注文は、最初のレベル 1 スナップショットが買値と売値の両方を配信した後にのみ発行されます。それまで、元の MQL バージョンがターミナルの引用符に依存していたのと同様に、スクリプトは待機します。
- When only one side of the market satisfies the condition (for example, `ZenPrice` is above ask but not below bid), the other protective order is cancelled to avoid stale prices.
- All comments inside the code are in English, while this documentation is provided in multiple languages in accordance with the project guidelines.

## MetaTrader スクリプトとの違い

- 元のスクリプトは、既存のポジションチケットのストップロスフィールドとテイクプロフィットフィールドを変更します。 StockSharp は保護注文を明示的な逆指値注文および指値注文として公開するため、変換は代わりに取引所に表示される注文で行われます。
- MetaTrader は、価格をブローカーの精度に自動的に合わせます。このポートでは、同じ動作が `NormalizePrice` を介して再現され、シンボルの価格ステップと小数点設定が活用されます。
- Position volume is rounded to exchange lot limits before sending the protective orders, ensuring compatibility with venues that require specific lot steps.
