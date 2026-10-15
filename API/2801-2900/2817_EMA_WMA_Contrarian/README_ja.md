# EMA WMA 逆張り戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足の始値に基づいて構築された指数移動平均（EMA）と加重移動平均（WMA）を比較する逆張りクロスオーバーシステムです。速いEMAがWMAを下抜けすると、戦略は反発を賭けて買います。EMAが再びWMAを上抜けすると、ショートに入ります。取引サイズは設定されたリスクパーセンテージと保護ストップまでの距離から算出され、オプションのストップロス、テイクプロフィット、トレーリングストップレベルがエクスポージャーをコントロールします。

## 詳細

- **エントリー条件**:
  - ロング: EMA(始値)がWMA(始値)を上から下へクロス
  - ショート: EMA(始値)がWMA(始値)を下から上へクロス
- **ロング/ショート**: 両方向
- **エグジット条件**:
  - 価格ステップ単位の固定ストップロス
  - 価格ステップ単位の固定テイクプロフィット
  - `TrailingStopPoints + TrailingStepPoints` だけ価格が動いた後に進むトレーリングストップ
  - 逆クロスにより現在のポジションをクローズして新しいポジションを開く
- **ストップ**: ストップロス、テイクプロフィット、トレーリングストップ
- **デフォルト値**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: 移動平均、逆張り
  - 方向: ロング & ショート
  - インジケーター: EMA (始値)、WMA (始値)
  - ストップ: はい（ハードストップ、トレーリング）
  - 複雑さ: 中級
  - 時間軸: イントラデイ（デフォルト1分）
  - 季節性: なし
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | ローソク足の始値で計算されたEMAとWMAのルックバック期間。 |
| `StopLossPoints`, `TakeProfitPoints` | 保護ストップロスと利益目標を配置する価格ステップ単位の距離。 |
| `TrailingStopPoints` | 有効化後の価格とトレーリングストップ間の距離。 |
| `TrailingStepPoints` | トレーリングストップが引き上げ/引き下げされる前に必要な追加の有利な動き。トレーリングが有効な場合は正である必要があります。 |
| `RiskPercent` | 1トレードにリスクするポートフォリオ資本の割合。ポジションサイズは `RiskPercent / (StopLossPoints * PriceStep)` として計算されます。 |
| `BaseVolume` | リスクベースのサイジングが決定できない場合に使用される最小取引サイズ。 |
| `CandleType` | 計算用のローソク足データタイプ（デフォルト1分）。 |

## 注意事項

- 両方の移動平均はローソク足の始値を消費し、オリジナルのMetaTraderエキスパートアドバイザーを反映しています。
- トレーリングストップは、取引に有利な方向に少なくとも `TrailingStopPoints + TrailingStepPoints` だけ価格が動いた後にのみ作動し、レガシーロジックを再現します。
- `TrailingStopPoints` が設定されている一方で `TrailingStepPoints` がゼロまたは負の場合、矛盾したトレーリング動作を避けるために戦略は即座に停止します。
- ポートフォリオ価値、価格ステップ、またはストップ距離が利用できない場合、リスクベースのサイジングは `BaseVolume` にフォールバックします。
