# 3103 — ADX EA (C#) 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
オリジナルのMetaTrader「ADX EA」は、Average Directional Indexのブレイクアウトと+DI/−DIのクロスオーバー、上位タイムフレームの
モメンタム確認、月次MACDフィルターを組み合わせています。C#ポートはStockSharpの高レベルAPIの上でそのマルチフィルターワーク
フローを複製します。戦略は3つの足ストリームを購読します：

1. **主要タイムフレーム**（デフォルト5分） — ADX、線形加重移動平均、価格構造チェック、ボリュームフィルターを駆動します。
2. **モメンタムタイムフレーム**（デフォルト15分） — エントリーを制御する100ベースライン周辺のモメンタム偏差を生成します。
3. **MACDタイムフレーム**（デフォルト30日） — ポジション決済を制御する月次MACDを反映します。

## 取引ロジック
- **ブレイクアウトモジュール** – 有効時、ロング取引には以下が必要です：
  - ADXまたは+DIが`EntryLevel`を上回り、+DIと−DIの差が`MinDirectionalDifference`より大きい。
  - 高速LWMAが低速LWMAを上回り、強気足構造（`Low[2] < High[1]`）、モメンタム上昇（`Momentum[1] > Momentum[2]`）。
  - 上位タイムフレームの直近3回のモメンタム読み取りのうち少なくとも1回が100から`MomentumBuyThreshold`以上乖離している。
  - 主要タイムフレームで出来高増加（`Volume[1] > Volume[2]`または`Volume[1] > Volume[3]`）。
  - 月次タイムフレームのMACDが強気（`MacdMain[1] > MacdSignal[1]`）。
  - 全体的なトレンド強度を確認するためADXが`ExitLevel`を上回る。

  ショートブレイクアウトは、−DI優位、弱気構造（`Low[1] < High[2]`）、`MomentumSellThreshold`分の100以下のモメンタム、
  弱気MACDの比較で対称的なロジックを適用します。

- **クロスオーバーモジュール** – 有効時、+DIが−DIを上回るクロス（ロング）または−DIが+DIを上回るクロス（ショート）を探します。
  オプションのフィルターはオリジナルのEAを反映します：
  - `RequireAdxSlope`はADXが前回読み取りより高いことを要求します。
  - `ConfirmCrossOnBreakout`はクロスバーに同じブレイクアウト閾値チェックを追加します。
  - `MinAdxMainLine`はクロス中の最小ADX強度を強制します。
  - LWMAの整列、モメンタムの傾き、出来高の拡大、MACDの極性が意図した方向に一致している必要があります。

- **ピラミッディング** – 各新規注文は`LotExponent`に従ってボリュームを追加します。戦略は`TradeVolume`をベースロットサイズ
  として扱い、`LotExponent^n`（`n`はすでに開いているステップ数）で増やします。`MaxTrades`は蓄積できるネットボリュームを制限します。

## リスク管理
- **保護注文** – `TakeProfitSteps`と`StopLossSteps`は`StartProtection`に渡され、銘柄の価格ステップで表現されます。
- **トレーリングストップ** – `TrailingStopSteps`は最高の終値を超えた手動トレーリングバリアを維持します。
- **ブレイクイーブン** – `UseBreakEven`が有効の場合、価格が`BreakEvenTrigger`ステップ前進した後にストップが締め付けられ、
  `BreakEvenOffset`ステップだけストップをオフセットできます。
- **MACD決済** – `EnableMacdExit`が真の場合、月次MACD関係はMACDがシグナルを下回ったときにロングを閉じます（ショートはその逆）、
  EAの`Close_BUY`/`Close_SELL`ルーティンに対応します。
- **資本ストップ** – `UseEquityStop`は浮動利益曲線を追跡し、ドローダウンが`TotalEquityRisk`パーセントに達するとポジションを
  清算します。

口座通貨ターゲットに依存する機能（「Take Profit in Money」、「Trailing Profit in Money」など）はポートされていません。
StockSharp戦略は通常、ストップ距離と組み込み保護サービスを通じて保護ロジックを管理するためです。EAの他のすべての決定ポイントは
インジケーター等価物で保持されます。

## パラメーター
| パラメーター | デフォルト | 説明 |
|-------------|-----------|------|
| `TradeVolume` | 0.01 | 最初のエントリーのベースロットサイズ。 |
| `CandleType` | 5mタイムフレーム | ADX/LWMAロジックの主要足シリーズ。 |
| `MomentumCandleType` | 15mタイムフレーム | モメンタム偏差フィルターの上位タイムフレーム。 |
| `MacdCandleType` | 30日タイムフレーム | MACD決済フィルターを供給するタイムフレーム。 |
| `FastMaPeriod` | 6 | 高速線形加重移動平均の長さ。 |
| `SlowMaPeriod` | 85 | 低速線形加重移動平均の長さ。 |
| `AdxPeriod` | 14 | Average Directional Indexの期間。 |
| `MomentumPeriod` | 14 | 上位タイムフレームのモメンタムインジケーター期間。 |
| `MacdFastPeriod` | 12 | MACD決済フィルター内の高速EMA期間。 |
| `MacdSlowPeriod` | 26 | MACD決済フィルター内の低速EMA期間。 |
| `MacdSignalPeriod` | 9 | MACD決済フィルター内のシグナルSMA期間。 |
| `EnableBreakoutStrategy` | true | ADXブレイクアウトブランチのトグル。 |
| `EnableCrossStrategy` | true | DIクロスオーバーブランチのトグル。 |
| `UseTrendFilter` | true | ブレイクアウト中にロングで+DI優位、ショートで−DI優位を強制。 |
| `RequireAdxSlope` | true | DIクロスを評価する際にADXが上昇することを要求。 |
| `ConfirmCrossOnBreakout` | true | クロスオーバーモジュールにブレイクアウト閾値を追加。 |
| `EnableMacdExit` | true | MACDベースの決済ルーティンを有効化。 |
| `EntryLevel` | 10 | ブレイクアウトで使用される最小ADX/+DI/−DIレベル。 |
| `ExitLevel` | 10 | 新規エントリーを許可する最小ADX強度。 |
| `MinDirectionalDifference` | 10 | +DIと−DIの必要差。 |
| `MinAdxMainLine` | 10 | DIクロス中の最小ADXレベル。 |
| `MomentumBuyThreshold` | 0.3 | 強気モメンタム確認に必要な100からの偏差。 |
| `MomentumSellThreshold` | 0.3 | 弱気モメンタム確認に必要な100からの偏差。 |
| `MaxTrades` | 10 | 最大ピラミッディングステップ数。 |
| `LotExponent` | 1.44 | 各追加ステップのボリューム乗数。 |
| `TakeProfitSteps` | 50 | テイクプロフィット注文の価格ステップ単位の距離。 |
| `StopLossSteps` | 20 | ストップロス注文の価格ステップ単位の距離。 |
| `TrailingStopSteps` | 40 | 価格ステップ単位の手動トレーリングストップ距離。 |
| `UseBreakEven` | true | ブレイクイーブン再配置ロジックを有効化。 |
| `BreakEvenTrigger` | 30 | ブレイクイーブンを有効にする前に必要な有利な動きのステップ数。 |
| `BreakEvenOffset` | 30 | ストップ移動時にエントリー価格に追加されるステップ数。 |
| `UseEquityStop` | true | ドローダウンベースの緊急決済を有効化。 |
| `TotalEquityRisk` | 1 | すべてのポジションをフラットにする前の許容ドローダウンパーセンテージ。 |

## 使用のヒント
- 元のタイムフレームマッピングを模倣するために`MomentumCandleType`と`MacdCandleType`を主要タイムフレームに合わせてください
  （例：5分チャート → 15分モメンタム → 月次MACD）。
- `EntryLevel`、`MinDirectionalDifference`、`MinAdxMainLine`を一緒に調整してください；三つ全てを下げるとブレイクアウト
  フィルターがかなり緩くなります。
- `LotExponent`を1.0より大きくするとEAのマーチンゲール式スケーリングが再現されます。ポジションサイズを一定に保つには1.0に設定。
