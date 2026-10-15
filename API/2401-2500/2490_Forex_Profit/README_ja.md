# Forex Profit戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderのエキスパートアドバイザー「Forex Profit」の翻訳。戦略は3本の指数移動平均の並びとParabolic SARの確認を待ち、完了した各ローソク足のクローズで取引に入ります。リスクは非対称のストップロスとテイクプロフィット距離、トレーリングストップ、EMAベースの追加利益ロックで管理されます。

## 詳細

- **エントリー条件**:
  - ロング: `EMA10` が `EMA25` と `EMA50` の両方を上回り、前のバーの `EMA10` が `EMA50` 以下で、Parabolic SARが前のクローズ以下。
  - ショート: `EMA10` が `EMA25` と `EMA50` の両方を下回り、前のバーの `EMA10` が `EMA50` 以上で、Parabolic SARが前のクローズ以上。
  - シグナルは完了したローソク足ごとに1回のみ評価されます。
- **エグジット条件**:
  - `EMA10` が前の値を下回り *かつ* 現在の利益が `ProfitThreshold` を超えたときにロングをクローズ。
  - `EMA10` が前の値を上回り *かつ* 現在の利益が `ProfitThreshold` を超えたときにショートをクローズ。
  - 注文エントリー時に設定した保護的ストップロスとテイクプロフィットレベル（ロングとショートで異なる距離）。
  - トレーリングストップは価格がエントリーから `TrailingStopPoints` 動いた後に有効になり、`TrailingStepPoints` 単位で更新されます。
- **ストップ**: はい — 固定ストップロス、固定テイクプロフィット、トレーリングストップ管理。
- **デフォルト値**:
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = 1時間時間軸
- **追加メモ**:
  - ストップ/ターゲット距離は銘柄の価格ステップで表され、銘柄のティックサイズを使用して自動的に変換されます。
  - 利益ベースのエグジットは、ポジションの総利益（ボリュームを含む）に依存し、価格ティックから口座通貨に変換されます。
  - トレーリングロジックは設定されたステップを超えることなく価格の動きの後ろにストップを保持します。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: EMA、Parabolic SAR
  - ストップ: はい（固定 + トレーリング）
  - 複雑さ: 中級
  - 時間軸: 設定可能（デフォルト1時間）
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
