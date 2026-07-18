# Reversals With Pin Bars戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTraderエキスパートアドバイザー**「Reversals With Pin Bars」**のC#移植版です。元のEAは、長いヒゲを持つ反転拒否ローソク足（ピンバー）を探し、モメンタムフィルター、上位時間軸の線形加重移動平均（LWMA）トレンド確認、MACD方向フィルターで確認します。この移植版は複数時間軸構造を維持し、StockSharp指標のみを使用し、重要なリスク制御をパラメーターとして公開します。

実装はStockSharpの高レベルAPIに焦点を当てています。主時間軸のローソク足がエントリーを駆動し、追加購読が上位時間軸の指標へデータを供給します。リスク管理はpipsで表され、任意のトレーリングストップとブレイクイーブン自動化をサポートします。

## エントリーロジック
- **ピンバー検出**: 直前の確定ローソク足は、全レンジの少なくとも50%を占めるヒゲを持つ必要があります。
  - ロング設定: 上ヒゲが支配的（元の「hanging man」チェックに対応）。
  - ショート設定: 下ヒゲが支配的。
- **トレンドフィルター**: 上位時間軸で高速LWMA（長さ = `FastMaPeriod`）が低速LWMA（`SlowMaPeriod`）の上/下にある必要があります。
- **モメンタムフィルター**: 上位時間軸の直近3バーのいずれかで、モメンタム値の100からの絶対距離が`MomentumThreshold`を超える必要があります。
- **MACDフィルター**: MACD時間軸でMACDメインラインがシグナルラインの上/下にある必要があります。
- **ポジション制限**: ネットエクスポージャーは`MaxTrades * Volume`を超えられません。新しい取引は整合済みの`Volume`設定を使用します。

## リスク管理
- **Stop-loss / Take-profit**: エントリー終値からの固定pip距離（`StopLossPips`、`TakeProfitPips`）。
- **ブレイクイーブン**: 有効な場合、価格が`BreakEvenTriggerPips`進むと、ストップを`entry +/- BreakEvenOffsetPips`へ移動します。
- **トレーリングストップ**: 有効な場合、直近終値から`TrailingStopPips`の距離を維持します。
- **自動フラット化**: 計算されたストップまたは目標に到達すると、成行注文でポジション全体を決済します。

## パラメーター
| パラメーター | 説明 |
| --- | --- |
| `TradeVolume` | 各新規エントリーで使う数量。銘柄ステップに合わせます。 |
| `MaxTrades` | 同方向エントリーの最大数（集約数量の制限）。 |
| `StopLossPips` | ストップロス距離（pips）。 |
| `TakeProfitPips` | テイクプロフィット距離（pips）。 |
| `EnableTrailing` / `TrailingStopPips` | トレーリングストップ距離を有効化し設定します。 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | ブレイクイーブン起動とバッファー設定。 |
| `FastMaPeriod` / `SlowMaPeriod` | 上位時間軸LWMAの長さ。 |
| `MomentumPeriod` / `MomentumThreshold` | モメンタム長と100からの最小絶対距離。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | 長期フィルター用MACD設定。 |
| `CandleType` | ピンバー検出用の主ローソク足系列。 |
| `HigherCandleType` | LWMAとモメンタムに使うローソク足系列。 |
| `MacdCandleType` | MACDに使うローソク足系列。 |

## MetaTrader版との差異
- 金額ベースのテイクプロフィット、トレーリング、エクイティストップのオプションは省略され、リスクはpipsで表されます。
- チャートオブジェクトを必要としたフラクタルライン確認は、指標ベースの条件に置き換えられました。
- すべての通知（アラート、メール、プッシュメッセージ）は削除され、StockSharp版は取引ロジックに集中します。

## 使用上の注意
1. 戦略をポートフォリオと銘柄へ割り当て、希望する複数時間軸設定に合わせて3つのローソク足タイプを調整します。
2. 銘柄の価格ステップがpip定義を反映していることを確認してください（既定フォールバックは0.0001）。
3. 戦略を開始します。ストップ、目標、トレーリング、ブレイクイーブン管理はローソク足終値で自動実行されます。
4. 結果を監視し、銘柄のボラティリティ特性に合わせてモメンタムとLWMAの長さを調整します。
