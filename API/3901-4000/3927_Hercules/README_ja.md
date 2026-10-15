# ヘラクレス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Hercules 戦略は、MetaTrader エキスパート **Hercules v1.3 (Majors)** の StockSharp 移植です。高速/低速移動平均クロスオーバーとマルチタイムフレーム確認フィルターを組み合わせ、シグナルごとに 2 つの独立した利益目標を実行します。

## 取引ロジック

* **シグナルアーム** – ローソク足のクローズ時に高速の EMA (デフォルトは 1 期間) を計算し、ローソク足のオープン時に低速の SMA (72 期間) を計算します。最後または最後から 2 番目のバーで発生したクロスオーバーを検出します。クロスオーバー価格は両方の移動平均で平均され、トリガーレベルは `TriggerPips` 上 (ロングの場合) または下 (ショートの場合) に配置されます。
* **実行ウィンドウ** – クロスオーバーが検出されると、セットアップは 2 つのバー全体にわたって有効のままになります。現在の終値がこのウィンドウ内のトリガー価格を超えた場合にのみ、注文の発行が許可されます。
* **フィルター** –
  * H1 RSI (デフォルトの長さ 10、通常の価格入力) はロングの場合は `RsiUpper` より大きく、ショートの場合は `RsiLower` より小さい必要があります。
  * 現在の終値は、取引時間枠で `LookbackMinutes` 個のローソク足で収集された最近の高値/安値を破る必要があります。
  * 日次エンベロープ (SMA 24、±`DailyEnvelopeDeviation`%) では、価格が取引方向のバンドの外側で終了する必要があります。
  * H4 エンベロープ (SMA 96 ±`H4EnvelopeDeviation`%) は、2 番目のより高いタイムフレームの確認を追加します。
* **リスク管理** – ストップロスは、ローソク足 4 本前のバーの高値/安値に設定されます。出来高は固定（`OrderVolume`）することも、現在のポートフォリオ値の `RiskPercent` から再計算することもできます。
* **取引管理** – 各シグナルにより、同じ量の 2 つの成行注文がオープンされます。 1 つ目は `TakeProfitFirstPips` で清算され、2 つ目は `TakeProfitSecondPips` で清算されます。末尾のストップを `TrailingStopPips` にすると、両方の注文が保護されます。ストップまたは両方のターゲットが完了すると、ストラテジーは `BlackoutHours` のブラックアウト期間に入り、その間は新しい取引は行われません。

## パラメーター

| パラメータ | 説明 |
| --- | --- |
| `OrderVolume` | 資金管理調整前の各成行注文の出来高。 |
| `UseMoneyManagement` | 有効にすると、ポートフォリオの `RiskPercent` と現在の停止距離から体積が再計算されます。 |
| `RiskPercent` | 設定ごとのリスクに対するポートフォリオの価値の割合。 |
| `TriggerPips` | エントリーを許可するために超える必要があるクロスオーバー価格からの距離。 |
| `TrailingStopPips` | 組み合わせたポジションに適用されるピップ単位のトレーリングストップ距離。 |
| `TakeProfitFirstPips` | 最初の部分的なテイクプロフィットのピップ距離。 |
| `TakeProfitSecondPips` | 2 番目の部分的なテイクプロフィットのピップ距離。 |
| `FastPeriod` | 高速 EMA トリガー ラインの長さ。 |
| `SlowPeriod` | 遅い SMA ベースラインの長さ。 |
| `RsiPeriod` | RSI 確認フィルターの長さ。 |
| `RsiUpper` / `RsiLower` | RSI のしきい値によりロングおよびショートの取引が可能になります。 |
| `LookbackMinutes` | 最近の高/低ブレイクアウト フィルターを計算するために使用されるウィンドウ (分単位)。 |
| `BlackoutHours` | 実行後に新しいセットアップを受け入れるまでに数時間の一時停止が必要です。 |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | 日次エンベロープフィルターのパラメーター。 |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | H4 エンベロープ フィルターのパラメーター。 |
| `CandleType` | 取引執行に使用される主な時間枠。 |
| `RsiTimeFrame` | RSI インジケーターをフィードするタイムフレーム。 |
| `DailyTimeFrame` | 毎日のエンベロープ計算をフィードする時間枠。 |
| `H4TimeFrame` | H4 エンベロープ計算をフィードするタイムフレーム。 |

## ファイル

* `CS/HerculesStrategy.cs` – Hercules 戦略の C# 実装。
* `README.md` – この文書。
* `README_ru.md` – ロシア語の説明。
* `README_zh.md` – 中国語の説明。
