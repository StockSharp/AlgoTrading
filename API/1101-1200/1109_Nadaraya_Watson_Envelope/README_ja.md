# Nadaraya-Watson エンベロープ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

対数スケールでNadaraya-Watsonカーネル回帰エンベロープを構築します。価格が下部エンベロープを上抜けたときにロングを取り、オプションで上部エンベロープを下抜けたときにショートを取ります。

## 詳細

- **エントリー条件**:
  - 終値が下部エンベロープを上抜けたときにロング。
  - 終値が上部エンベロープを下抜けたときにショート（ロング/ショートモード時）。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: 逆方向のエンベロープクロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: Envelope
  - 方向: 設定可能
  - インジケーター: Nadaraya-Watson
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
