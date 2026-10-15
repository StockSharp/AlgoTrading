# トレンドタイプ・インジケーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Trend Type Indicator は ATR と ADX を使用して市場のレジームを検出します。
上昇トレンド中はロング、下降トレンド中はショート、横ばいになると決済します。

## 詳細

- **エントリー条件**: +DI が -DI より大きく、横ばいでないこと
- **ロング/ショート**: 両方
- **エグジット条件**: 逆トレンドまたは横ばい
- **ストップ**: なし
- **デフォルト値**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, ADX
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
