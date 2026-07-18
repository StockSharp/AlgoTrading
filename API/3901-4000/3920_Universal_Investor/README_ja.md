# 普遍的な投資家戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、**Universal Investor** MetaTrader 4 エキスパート アドバイザーを直接移植したものです。指数移動平均 (EMA) と線形加重移動平均 (LWMA) を組み合わせて短期トレンドの方向を確認し、適応的なポジションサイジングで 1 ポジション取引を実行します。

## 取引ロジック

1. 構成された `CandleType` をサブスクライブし、`MovingPeriod` で定義された期間で EMA と LWMA の両方を計算します。
2. ロジックが元の EA からの `iMA(..., shift = 1/2)` 呼び出しを模倣するように、各移動平均の 2 つの最新の値を保存します。
3. 以前の LWMA が前の EMA を上回り、両方の平均が上昇しており、同じローソク足に反対のシグナルがない場合、**買い** シグナルを生成します。
4. 以前の LWMA が前の EMA を下回り、両方の平均が下降しており、同じローソク足に反対のシグナルがない場合、**売り** シグナルを生成します。
5. LWMA が EMA を下回ったらすぐにオープン ロング ポジションを閉じます (ショートのミラー ロジック)。
6. 戦略 `Volume` パラメータから取引量を計算し、ポートフォリオ値が十分に大きい場合は `MaximumRisk` 要件を満たすように取引量を増やし、取引が連続して負けた後は `DecreaseFactor` に従って減少させます。
7. `BuyMarket`/`SellMarket` を使用して成行注文を送信し、エントリー価格を追跡してエグジットの勝敗を検出します。

この戦略は、一度に 1 つのポジションのみをオープンにし、完全にクローズした後にのみすぐに反転し、元の MetaTrader スクリプトの動作を再現します。

## パラメーター

| 名前 | 説明 |
| --- | --- |
| `CandleType` | 計算に使用されるローソク足シリーズ。 |
| `MovingPeriod` | EMA と LWMA の両方の期間。 |
| `MaximumRisk` | 最小ポジション量の計算に使用される資本の割合 (0.05 = 5%)。 |
| `DecreaseFactor` | 連続して負けた取引の後にボリュームを減らします (0 は機能を無効にします)。 |
| `Volume` | 基本契約量は `BuyMarket`/`SellMarket` に渡されました。 |

## インジケーター

- `ExponentialMovingAverage`
- `LinearWeightedMovingAverage`

## 注意事項

- 注文は、`Time[0]` チェックに依存する EA に一致する、クローズされたローソク足に対してのみ行われます。
- ポジション サイズのロジックは、リスクベースのコンポーネントと連敗乗数を含む、MetaTrader `LotsOptimized` 関数を反映しています。
