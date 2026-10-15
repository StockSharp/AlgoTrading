# カラー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

設定された色の知覚輝度に基づいて取引する戦略。
色が明るい（輝度 > 0.5）場合は買い、それ以外は売りを行います。

## 詳細

- **エントリー条件**:
  - ロング: `Color luminance > 0.5`
  - ショート: `Color luminance <= 0.5`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `ColorHex` = "#f23645"
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: その他
  - 方向: 両方
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
