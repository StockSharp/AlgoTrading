# Universum 3.0戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

DeMarkerオシレーターに基づく戦略で、完成した各バーでポジションを開き、マーチンゲール方式でボリュームを調整します。

## 詳細

- **エントリー条件**:
  - ロング: `DeMarker > 0.5`
  - ショート: `DeMarker < 0.5`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ポジションはテイクプロフィットまたはストップロスで決済されます
- **ストップ**: `TakeProfitPoints` と `StopLossPoints` による絶対ポイント
- **デフォルト値**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: DeMarker
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
