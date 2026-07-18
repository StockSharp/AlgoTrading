# コムフラクティ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

ComFracti は、MT4「ComFracti」エキスパートアドバイザーから翻訳された方向性戦略です。このロジックは、マルチタイムフレームのフラクタル確認を RSI および確率的フィルターと組み合わせ、オプションの移動平均、放物線 SAR、チャネルおよびパーセプトロン フィルターがトレンドの調整を制御します。 C# 実装は一度に 1 つのポジションを取引し、StockSharp の高レベル API を使用して完了したローソク足のシグナルを評価します。

## 取引ロジック

- **一次信号**
  - 現在の時間枠とより高い時間枠の両方が強気のフラクタルシグナルを生成する場合、強気のセットアップを確認します。
  - 両方のタイムフレームが弱気のフラクタル信号を生成する場合、弱気のセットアップを確認します。
  - RSI（上位タイムフレームのデフォルトの 3 期間）は、RSI フィルタが有効な場合、ロングの場合は `50 - RsiLevelBuy` を下回るか、ショートの場合は `50 + RsiLevelSell` を上回る必要があります。
  - 確率的フィルターが有効な場合、確率的オシレーター (デフォルトは %K 期間 5、%D 平滑化 3/3) は、ロングの場合は `50 - StochasticLevelBuy` 未満、ショートの場合は `50 + StochasticLevelSell` を超える必要があります。
- **オプションのフィルター**
  - **EMA の傾き**: フィルター時間枠の EMA は、ロングの場合は上昇し、ショートの場合は下降する必要があります。
  - **Parabolic SAR**: the SAR value must stay below the bar open for longs or above for shorts.
  - **Channel breakout**: compares the previous bar against an adaptive Donchian-style channel;ロングの場合は以前の安値がチャネルの床より上にとどまる必要があり、ショートの場合は以前の高値が天井より下にとどまる必要があります。
  - **パーセプトロン**: 最近の高値と安値の差の加重合計は、ロングの場合はプラス、ショートの場合はマイナスである必要があります。
- **ポジション管理**
  - 一度にアクティブになるポジションは 1 つだけです。この戦略では、反対方向に新しい取引を開始する前に、既存のエクスポージャーを閉じます。
  - 固定のストップロスとテイクプロフィットの距離は商品ポイントで表されます。
  - オプションのトレーリングストップは、トレーリングバッファーに達すると (`ProfitTrailing` が true の場合) 利益の方向に移動します。
  - `CloseOnOppositeSignal` が有効な場合、反対のプライマリ信号が表示された場合、戦略は早期に終了します。

## リスク管理

- Base position size equals the `BaseVolume` parameter (default 0.1 lots). `AccountMicro` が有効な場合、音量は 10 で分割されます。
- `UseMoneyManagement` が有効な場合、戦略は、設定されたストップロス距離と商品ステップ値を使用してポジション サイズを概算し、取引ごとの口座価値の `RiskPercent` をリスクにさらします。計算されたボリュームは `MinimumVolume` によってクランプされます。

## パラメーター

| 名前 | 説明 |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | 商品ポイントでのテイクプロフィットとストップロスの距離。 |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | トレーリングストップのコントロール（距離とトレーリングにオープン利益が必要かどうか）。 |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | ポジションサイジング構成。 |
| `UseFractals`, `FractalShift*` | フラクタル確認を有効にし、現在およびそれ以降のタイムフレームで検査するバーのオフセットを定義します。 |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI フィルターのオフセットと時間枠。 |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | Stochastic 発振器の周期としきい値。 |
| `UseMaFilter`, `MaPeriod` | フィルタ時間枠での EMA フィルタ構成。 |
| `UsePsarFilter`, `PsarStep` | Parabolic SAR フィルター構成。 |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | チャネルブレイクアウトフィルタパラメータ。 |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | パーセプトロン フィルターの重み (0 ～ 100、50 を中心)。 |
| `CandleType`, `HigherFractalType`, `FilterType` | 戦略で使用されるデータの時間枠。 |

## 注意事項

- The strategy processes finished candles only, so behaviour may differ slightly from the original tick-driven expert advisor.
- フラクタル トラッカーは MT4 の 5 バー フラクタル ロジックを再現し、ユーザーが MT4 `sh1/ sh2` パラメーターに合わせてどの履歴バーを評価するかをシフトできるようにします。
- 資金管理は、StockSharp 内で利用可能なポートフォリオ評価に依存します。利用可能な評価がない場合、戦略は固定ベースボリュームに戻ります。
