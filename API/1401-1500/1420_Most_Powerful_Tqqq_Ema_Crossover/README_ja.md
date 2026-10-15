# 最強のTQQQ EMAクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期EMAが長期EMAを上抜けするとロングエントリーします。テイクプロフィットとストップロスはエントリー価格の乗数として設定されます。

## 詳細

- **エントリー条件**: 短期EMAが長期EMAを上抜けする
- **ロング/ショート**: ロングのみ
- **エグジット条件**: テイクプロフィットまたはストップロスレベルへの到達
- **ストップ**: はい（固定乗数）
- **デフォルト値**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
