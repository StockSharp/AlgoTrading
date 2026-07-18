# Ichimoku出来高クラスター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Ichimoku Volume Cluster**戦略は、出来高クラスターの確認を伴う一目均衡表を中心に構築されています。

インジケーターがイントラデイ(1h)データでトレンド変化を確認したときにシグナルが発動します。これにより、この手法はアクティブトレーダーに適しています。

ストップはATRの倍数とTenkanPeriod、KijunPeriodなどの要素に基づいています。デフォルト値を調整してリスクとリワードのバランスを取ってください。

## 詳細
- **エントリー条件**: インジケーター条件の実装を参照。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対シグナルまたはストップロジック。
- **ストップ**: はい、インジケーターに基づく計算を使用。
- **デフォルト値**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `VolumeAvgPeriod = 20`
  - `VolumeStdDevMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromHours(1).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数のインジケーター
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
