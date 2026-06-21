# Gold ブレイクアウト RR4戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Gold Breakout RR4は、ボリュームとLWTIトレンドフィルターを使用して金のDonchianチャンネルブレイクアウトを取引します。指定されたセッション内で1日1トレードのみ行い、固定の4:1リスク/リワードを使用します。

## 詳細

- **エントリー条件**: セッション内でDonchianチャンネルを平均以上のボリュームとLWTI確認でブレイクアウト
- **ロング/ショート**: 両方
- **エグジット条件**: リスク/リワードに基づく固定ストップと目標
- **ストップ**: はい
- **デフォルト値**:
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Donchian Channel, SMA, WMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
