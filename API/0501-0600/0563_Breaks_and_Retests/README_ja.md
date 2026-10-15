# ブレイクアウトとリテスト
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

最近の高値・安値のブレイクアウトとオプションのリテストでエントリーし、トレーリングストップで管理する戦略です。

アプローチは、ルックバックウィンドウにおける最高値と最安値のクローズで定義されたサポートとレジスタンスを追跡します。ブレイクアウトはブレイク方向にポジションを開くか、または破られたレベルのリテストを待ちます。エグジットは初期ストップロスを使用し、利益が閾値に達するとトレーリングストップに変わります。

## 詳細

- **エントリー条件**: レジスタンス上方またはサポート下方へのブレイクアウト、オプションのリテスト。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: トレーリングストップまたは逆方向ブレイクアウト。
- **ストップ**: 初期ストップロスとトレーリングストップ。
- **デフォルト値**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
