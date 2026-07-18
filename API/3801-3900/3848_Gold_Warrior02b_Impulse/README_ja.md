# GoldWarrior02b 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTrader 4 エキスパート アドバイザー *GoldWarrior02b* の包括的な StockSharp ポート (フォルダー `MQL/7694`)。
It blends a Commodity Channel Index (CCI), a custom impulse gauge and a handcrafted ZigZag swing detector
and evaluates signals only a few seconds before every 15 minute boundary.この翻訳の目標は、
to mimic the high-level logic of the original robot while respecting StockSharp's net-position execution model.

## 主な特徴

- **Impulse filter** – replaces the `DayImpuls` custom indicator by averaging the candle open/close distance
商品の価格ステップによって正規化されます。
- **ジグザグ構造** – 最近のスイング高値と安値を再構築して、市場が上昇傾向にあるのか下降傾向にあるのかを判断します。
- **Timing gate** – entries are allowed only when the current candle closes during the last 15 seconds of minutes 14, 29, 44 or 59.
- **リスク管理** – ストップロス、テイクプロフィット、トレーリングストップ（オプション）および測定されたアカウント全体の利益目標が含まれます
通貨単位で。デフォルトは、MetaTrader 入力をミラーリングします (1,000 ポイントのストップ、150 ポイントのテイクプロフィット、トレーリングは無効)。
- **Net exposure** – StockSharp keeps a single net position per security, so the multi-level hedging and lot scaling
from the MQL implementation are not reproduced.代わりに、この戦略は単一のエントリーボリュームに焦点を当てています。

## 取引ロジック

### 信号の準備

1. Subscribe to candles defined by `CandleType` (5 minute timeframe by default).
2. Calculate CCI and the impulse average using the shared `ImpulsePeriod` (default 21 bars).
3. Update the ZigZag swing direction once the deviation exceeds `ZigZagDeviation` points and the depth/backstep
制約が満たされています。
4. Store the previous values of the indicators to replicate the "current" (`cci0`, `imp`) and "previous" (`cci1`, `nimp`)
エキスパートアドバイザーで使用されるバッファー。

### エントリールール

セットアップは、現在オープンなポジションがなく、最後の決済から少なくとも 15 秒が経過し、かつ
`AllowEntryTime` returns `true` (end of the 15 minute block).

**長い:**
- 最新のジグザグ スイングは下向きを指します (新しい安値は以前の安値よりも低い)。
- どちらか
  - 現在の CCI は前のバーと比較して増加しており、前の CCI は -50 未満であり、現在の CCI は -30 未満のままです。
the impulse turns positive and the previous impulse was negative;または
  - current CCI is below -200, the previous CCI was still lower, the impulse remains below `ImpulseBuyThreshold`
そして前の衝動よりも強いです。

**短い:**
- 最新のジグザグ スイングは上向きを指します (新しい高値は以前の高値よりも高くなります)。
- どちらか
  - 現在の CCI は前のバーと比べて減少しています。前のバー CCI は 50 を超えています。現在の CCI は 30 を超えています。
インパルスは負に変わり、前のインパルスは正でした。または
  - current CCI is above 200, the previous CCI was higher, the impulse stays above `ImpulseSellThreshold`
そして前の衝動よりも弱いです。

以前のインパルス値が `ImpulseSellThreshold` と `ImpulseBuyThreshold` の間にある場合、信号は無視されます。

### 出口管理

- **ストップロス** – 価格がエントリー価格（デフォルトでは 1,000 ポイント）を `StopLossPoints` 超えたときにトリガーされます。
- **利益確定** – `TakeProfitPoints` (150 ポイント) を移動した後にポジションを決済します。
- **トレーリングストップ** – オプション。有効にすると、価格が変動した後に有効になります `TrailingStopPoints + TrailingStepPoints`
ポジションを支持し、価格を `TrailingStopPoints` だけ引き離します。
- **利益目標** – `PriceStep` と `StepPrice` を使用して未決済損益を口座通貨に変換し、
`ProfitTarget` (デフォルトは 300) を超えるとポジションをクローズします。

## パラメーター

| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `BaseVolume` | エントリーの取引サイズ。 | `0.1` |
| `StopLossPoints` | 停止距離 (ポイント単位)。 | `1000` |
| `TakeProfitPoints` | テイクプロフィット距離（ポイント単位）。 | `150` |
| `TrailingStopPoints` | トレーリング ストップの距離 (ポイント単位) (0 はトレーリングを無効にします)。 | `0` |
| `TrailingStepPoints` | トレーリングが有効になるまでの追加距離。 | `0` |
| `ImpulsePeriod` | Period for both CCI and impulse calculations. | `21` |
| `ZigZagDepth` | 新しいジグザグ スイング間の最小バー数。 | `12` |
| `ZigZagDeviation` | スイングを確認するための最小値動き（ポイント単位）。 | `5` |
| `ZigZagBackstep` | 新しいスイングを受け入れるまでの最小バー数。 | `3` |
| `ProfitTarget` | 未実現利益のしきい値 (アカウント通貨)。 | `300` |
| `ImpulseSellThreshold` | ショートに必要な最小インパルス値。 | `-30` |
| `ImpulseBuyThreshold` | ロングに許可される最大インパルス値。 | `30` |
| `CandleType` | 作業時間枠。 | `5 minute time frame` |

## オリジナルの Expert Advisor との違い

- MetaTrader バージョンは、`GlobalVariableSet` を使用して注文をレート制限し、ヘッジ グリッドのチケット カウントを保存します。
このポートは時間ベースのスロットルを保持しますが、StockSharp アカウントのため、平均化/ヘッジ ラダーは保持しません
網がかかっている。
- 注文管理は、ハイレベルの API ガイダンスの範囲内に収まるように、成行注文 (`BuyMarket`、`SellMarket`) によって処理されます。
- インパルスの計算が簡素化されています。元の `DayImpuls` は 2 つのバッファ (`imp`、`nimp`) を公開します。ここでは両方のバッファ
は、現在および以前の移動平均読み取り値によって近似されます。

## 使用のヒント

- 最適化中に使用されるタイムフレームに一致するように `CandleType` を構成します (元の EA は M5 で動作します)。
- ポイント距離を正しく変換するために、機器が `PriceStep` および `StepPrice` メタデータを提供していることを確認してください。
- 現実的な滑り/待ち時間を使用してバックテストを行い、入場ゲート (15 分前の最後の数秒) が期待どおりに動作することを確認します。

## 免責事項

この戦略は教育目的で提供されています。事前に履歴データと将来データを使用して徹底的にテストしてください
実質資本を危険にさらすことになります。
