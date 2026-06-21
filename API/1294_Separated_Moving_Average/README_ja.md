# 分離移動平均戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

強気の終値と弱気の終値に対して別々の移動平均を構築します。強気平均が弱気平均を上抜けるとロングポジションを開き、逆のクロスでショートポジションを開きます。SMA、EMA、HMA をサポートし、Heikin Ashi 価格でも動作します。

## 詳細

- **エントリー条件**: 強気平均が弱気平均を上抜けるクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆クロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, EMA, HMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

