# The Puncher戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 5のエキスパートアドバイザー「The Puncher」から変換。
- 長期間のストキャスティクスオシレーターとRSIを組み合わせて枯渇ゾーンを特定します。
- StockSharpの高レベルAPIアプローチに従い、現在のローソク足が確定した時のみ取引します。
- 保護的なストップロス、テイクプロフィット、損益分岐点、トレーリングストップのロジックを適用してリスクを管理します。

## インジケーター
- **ストキャスティクスオシレーター**: ベース期間 `StochasticPeriod`、%Kスムージング `StochasticSignalPeriod`、%Dスムージング `StochasticSmoothingPeriod`。
- **相対力指数 (RSI)**: 期間 `RsiPeriod`。

## パラメーター
| パラメーター | デフォルト値 | 説明 |
|-------------|-------------|------|
| `StochasticPeriod` | 100 | ストキャスティクスオシレーターのベース期間。 |
| `StochasticSignalPeriod` | 3 | %Kラインに適用するスムージング期間。 |
| `StochasticSmoothingPeriod` | 3 | %Dラインに適用するスムージング期間。 |
| `RsiPeriod` | 14 | RSIの計算長。 |
| `OversoldLevel` | 30 | ストキャスティクスとRSIが共有する売られすぎゾーン検出のしきい値。 |
| `OverboughtLevel` | 70 | ストキャスティクスとRSIが共有する買われすぎゾーン検出のしきい値。 |
| `StopLossPips` | 20 | ストップロス距離（pips）（0でストップロスを無効化）。 |
| `TakeProfitPips` | 50 | テイクプロフィット距離（pips）（0でテイクプロフィットを無効化）。 |
| `TrailingStopPips` | 10 | トレーリングストップ距離（pips）（0でトレーリングを無効化）。 |
| `TrailingStepPips` | 5 | トレーリングストップを再度調整するために必要な最小有利移動（pips）。 |
| `BreakEvenPips` | 21 | ストップを損益分岐点に移動するために必要な利益（pips）（0で無効化）。 |
| `CandleType` | 5分時間軸 | 計算に使用するローソク足タイプ。 |
| `Volume` | 戦略プロパティ | エントリーに使用する注文サイズ（戦略の `Volume` で設定）。 |

> **Pip処理**: Pipベースのパラメーターは `Security.PriceStep` を使用して絶対価格に変換されます。取引する銘柄に合わせて `Security.PriceStep` を調整してください。

## 取引ルール
### エントリー
- **ロング**: ストキャスティクスのシグナルラインとRSIの両方が `OversoldLevel` を下回り、既存のロングポジションがない場合。
- **ショート**: ストキャスティクスのシグナルラインとRSIの両方が `OverboughtLevel` を上回り、既存のショートポジションがない場合。
- ポジションが開いているときに逆のシグナルが現れた場合、戦略はポジションをクローズし、新しいエントリーを検討する前に次のローソク足まで待ちます。

### エグジットとリスク管理
- **ストップロス**: `StopLossPips` で定義された固定距離。
- **テイクプロフィット**: `TakeProfitPips` で定義された固定目標。
- **損益分岐点**: 利益が `BreakEvenPips` に達すると、ストップをエントリー価格に移動。
- **トレーリングストップ**: 価格が `TrailingStopPips` だけ有利に動いた後、ストップは市場を追従し、`TrailingStepPips` ごとに調整されます。
- **逆シグナル**: ストップや目標に達していなくてもエグジットを強制します。

## 備考
- StockSharpでサポートされているあらゆる銘柄で機能します。デフォルト値はFXスタイルのpip値向けに調整されています。
- 完了したローソク足のみを使用し、元のロボットの `TradeAtCloseBar=true` の動作に一致します。
- 戦略を開始する前に、ポートフォリオ、銘柄、ボリュームを設定してください。
