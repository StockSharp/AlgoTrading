# SAR トレーリングシステム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

固定時間間隔でランダムなロングまたはショートポジションにエントリーし、Parabolic SARインジケーターを使用してエグジットを管理する戦略です。
Parabolic SARの値はトレーリングストップとして機能します。価格がSARレベルを超えたときにポジションがクローズされます。

## 詳細

- **エントリー条件**:
  - `TimerInterval`ごとに、オープンポジションがなく`UseRandomEntry`が有効な場合、ランダムなロングまたはショート取引が開かれます。
- **ロング/ショート**: 両方
- **エグジット条件**: 価格がParabolic SARを越えること。
- **ストップ**: TickでのInitialストップロスとParabolic SARトレーリングエグジット。
- **デフォルト値**:
  - `TimerInterval` = 300秒
  - `StopLossTicks` = 10
  - `AccelerationStep` = 0.02
  - `AccelerationMax` = 0.2
  - `UseRandomEntry` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
