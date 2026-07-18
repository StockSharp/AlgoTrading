# Heikin Ashi V2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashiシステムの第2バージョンは、EMAフィルターを追加します。Heikin Ashiローソク足の方向がEMAで定義されたトレンドと一致する場合にのみ取引を行います。このフィルターは、純粋なHAアプローチが生成する可能性があるトレンドに逆行するシグナルを避けるのに役立ちます。

## 詳細

- **エントリー条件**:
  - **ロング**: `HA_Close > HA_Open` かつ `Close > EMA`
  - **ショート**: `HA_Close < HA_Open` かつ `Close < EMA`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `EmaLength` = 20
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Heikin Ashi, EMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
