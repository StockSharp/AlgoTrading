# Bollinger RSI 逆張り SOL 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SOL向けの逆張りシステムで、価格がBollingerの下限バンドを上抜けかつRSIが低い時に買い、価格が上限バンドを下抜けかつRSIが高い時に売ります。平日のみ稼働します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格が下限バンドを上抜け、`RSI` < `Long RSI`（平日）。
  - **ショート**: 価格が上限バンドを下抜け、`RSI` > `Short RSI`（平日）。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - ロング: 価格が上限バンドを上抜け、または直近安値を下回るストップロス。
  - ショート: 価格が中央バンドを上抜け、または利益目標に到達。
- **ストップ**: ロングのストップは直近安値の下。
- **デフォルト値**:
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **フィルター**:
  - カテゴリ: Mean Reversion
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: はい（平日）
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
