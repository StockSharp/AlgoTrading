# ティックMarubozu戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ティックデータ上のMarubozu足を識別し、高出来高で確認します。強気Marubozuで買い、弱気Marubozuで売ります。

## 詳細

- **エントリー条件**: SMAを上回る出来高を伴う強気または弱気のMarubozu
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のシグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1-minute time frame
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
