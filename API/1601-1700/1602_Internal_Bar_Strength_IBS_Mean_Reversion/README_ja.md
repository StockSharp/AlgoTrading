# IBS 平均回帰戦略（Internal Bar Strength）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Internal Bar Strength（IBS）を使用したショート専用の平均回帰戦略です。IBS が高く価格が前回高値を上回ったときに売りを入れ、IBS が下限閾値を下回ったときに決済します。

## 詳細

- **エントリー条件**: IBS >= 上限閾値 かつ 終値 > 前回高値
- **ロング/ショート**: ショート
- **エグジット条件**: IBS <= 下限閾値
- **ストップ**: なし
- **デフォルト値**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ショート
  - インジケーター: IBS
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
