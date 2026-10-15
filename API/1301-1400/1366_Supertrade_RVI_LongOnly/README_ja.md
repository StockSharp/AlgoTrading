# Supertrade RVI ロングのみ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

相対ボラティリティ指数（RVI）が20を上回るクロスを利用してロングトレードを開きます。ストップロスとテイクプロフィットはリスクパーセントとリワード比率から設定されます。

## 詳細

- **エントリー条件**: RVIが閾値を上回るクロス
- **ロング/ショート**: ロング
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: はい
- **デフォルト値**:
  - `RviLength` = 10
  - `EmaLength` = 14
  - `RviThreshold` = 20
  - `RiskPercent` = 1
  - `RewardRatio` = 3
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: StdDev, EMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

