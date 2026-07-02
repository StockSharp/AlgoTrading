# 戦略 Universal Signal Demo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、StockSharp の高レベル API を使用して、MetaTrader 5「ユニバーサル シグナル」エキスパートを複製します。 8 つの重み付けされた市場パターンを評価し、それらを 1 つの複合スコアに集約します。スコアが設定可能なしきい値を超えると、ストラテジーはロングポジションとショートポジションをオープンまたはクローズし、オプションで設定されたバー数の後に期限切れになる保留中の指値注文を使用します。

## Strategy Parameters
- `CandleType` – 分析に使用されるローソク足データ。
- `SignalThresholdOpen` – ポジションをオープンするために必要な最小複合スコア。
- `SignalThresholdClose` – 既存のポジションから抜け出すには相手のスコアが必要です。
- `PriceLevel` – 保留中の指値エントリーを配置するための価格オフセット (0 は市場執行を意味します)。
- `StopLevel` / `TakeLevel` – 内蔵保護モジュールによって使用される絶対的なストップロスとテイクプロフィットの距離。
- `SignalExpiration` – まだアクティブな保留中のエントリがキャンセルされるまでのバーの数。
- `Pattern0Weight` … `Pattern7Weight` – 集計前に各パターンに適用される重み。
- `UniversalWeight` – すべてのパターン寄与の合計に適用される最終乗数。
- `ShortMaPeriod`、`LongMaPeriod`、`RsiPeriod`、`BollingerPeriod`、`BollingerWidth`、`TrendSmaPeriod`、`VolumeSmaPeriod` – パターン チェック内で使用されるインジケーター設定。

## 取引ロジック
1. 設定されたキャンドル ストリームをサブスクライブし、EMA、RSI、MACD シグナル、Bollinger バンド、およびサポートされる SMA をバインドします。
2. ローソク足が完成するたびに、8 つのブール パターン (トレンドの調整、RSI の勢い、MACD のヒストグラム、Bollinger の位置、ローソク足の方向、ボリュームの拡大) を計算します。
3. 各パターンにその重みを乗算し、寄与を合計し、全体的な重みを適用して最終スコアを取得します。
4. スコアが反対方向に決済しきい値を超えた場合、オープンポジションを決済します。
5. スコアが開始しきい値を超えたときに、新しいロング ポジションまたはショート ポジションをオープンします。 `PriceLevel` がプラスの場合、構成された距離だけオフセットされた指値注文を送信し、`SignalExpiration` バーの後に自動的にキャンセルします。
6. `StartProtection` は、StockSharp のリスク管理ヘルパーを使用して、すべてのポジションに対して固定のストップロスとテイクプロフィットのレベルを設定します。

この変換では、StockSharp のコーディング規約とインジケーターベースの処理に従いながら、元の MQL5 エキスパートの柔軟な重み付けワークフローが維持されます。
