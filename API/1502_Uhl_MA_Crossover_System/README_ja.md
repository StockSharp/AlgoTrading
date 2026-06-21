# Uhl MA クロスオーバー システム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Uhl MAクロスオーバーシステムは分散を使用してスムージングを調整し、2本のアダプティブライン（CTSとCMA）を構築する。CTSがCMAを上抜けるとロングポジション、下抜けるとショートポジションを建てる。

## 詳細

- **エントリー条件**: CTSがCMAを上抜ける。
- **ロング/ショート**: 両方。
- **エグジット条件**: CTSがCMAを下抜ける。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, Variance
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
