# 2 ペア相関戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**2 ペア相関戦略** は、MetaTrader エキスパート アドバイザー *「2 ペア相関 EA」* (パッケージ `MQL/52043`) を StockSharp の高レベル API に移植します。相関性の高い 2 つの暗号通貨シンボル (プライマリ レッグとして BTCUSD、ヘッジ レッグとして ETHUSD) の入札価格を監視し、スプレッドが設定可能なしきい値から逸脱した場合に市場中立取引を実行します。

### コアワークフロー

1. **リスクゲーティング** – ポートフォリオの資本は継続的に監視されます。過去のピークからのドローダウンが `MaxDrawdownPercent` を超えた場合、資本がピーク値の `RecoveryPercent` を超えて回復するまで、新しい取引は一時停止されます。
2. **ボラティリティフィルター** – どちらの金融商品も、5 分間のローソク足ストリームを長さ `AtrPeriod` の `AverageTrueRange` インジケーターに供給します。 ATR が `PriceDifferenceThreshold * 0.01` を超えると取引はスキップされ、MQL コードの「高ボラティリティ一時停止」を模倣します。
3. **スプレッド検出** – この戦略は両方の商品のレベル 1 データをサブスクライブし、更新ごとに入札価格のスプレッドを評価します。 `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold` の場合、BTCUSD が購入され、ETHUSD が販売されます。スプレッドが `-PriceDifferenceThreshold` を下回ると、ポジションが逆転します (BTCUSD のショート、ETHUSD のロング)。
4. **動的ロットサイジング** – レッグあたりの出来高は、現在のポートフォリオ株式の `RiskPercent` を合成ストップ距離 `StopLossPips * PriceStep` で割ったものから導出されます。結果は、注文が送信される前に、交換量の制約を使用して正規化されます。
5. **バスケットエグジット** – 両方のレッグの変動利益の合計が口座通貨で追跡されます。 `MinimumTotalProfit` に達すると、エントリー方向に関係なく、戦略はペア全体を閉じます。

## 必要な市場データ

- プライマリ証券 (`Security`) とヘッジ証券 (`SecondSecurity`) の両方の **レベル 1** (最良の買値/売値)。
- ATR フィルターに供給する同じ 2 つの金融商品のタイプ `AtrCandleType` (デフォルトは 5 分の時間枠) の **キャンドル**。

ロットサイジングと利益換算が MetaTrader の動作を反映するように、有価証券が意味のある `PriceStep`、`StepPrice`、`VolumeStep`、最小/最大出来高値を公開していることを確認します。

## パラメーター

| 名前 | 種類 | デフォルト | 説明 |
| ---- | ---- | ------- | ----------- |
| `SecondSecurity` | `Security` | — | ヘッジ手段（オリジナルの EA では ETHUSD）。 |
| `MaxDrawdownPercent` | `decimal` | `20` | 新しい取引を一時停止するドローダウンしきい値。 |
| `RiskPercent` | `decimal` | `2` | ポジションサイジングのための取引ごとのポートフォリオシェアのリスク。 |
| `PriceDifferenceThreshold` | `decimal` | `100` | ペアをオープンするには入札価格の乖離が必要です。 |
| `MinimumTotalProfit` | `decimal` | `0.30` | 両レッグをクローズするためのアカウント通貨での利益目標。 |
| `AtrPeriod` | `int` | `14` | ボラティリティフィルターの ATR の長さ。 |
| `RecoveryPercent` | `decimal` | `95` | ドローダウン後に取引を再開するために必要なピーク資本の割合。 |
| `StopLossPips` | `int` | `50` | `RiskPercent` をロットに変換するために使用される合成ストップ。 |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | ATR の計算に使用されるローソク足シリーズ。 |

## ファイル

- `CS/TwoPairCorrelationStrategy.cs` – 高レベルの API に基づいて構築された戦略の実装。
- `README.md` – このドキュメント (英語)。
- `README_zh.md` – 中国語のドキュメント。
- `README_ru.md` – ロシア語のドキュメント。
