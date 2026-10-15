# 4H Bollinger ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

4H Bollinger ブレイクアウト戦略は、4時間足チャートでBollinger Bandsのブレイクアウトを取引します。出来高とトレンドの確認とともに価格が下部バンドを上抜けた際にロングポジションを建てます。価格が上部バンドを下抜けてRSIが閾値を下回った際にショートポジションを建てます。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値が下部バンドを上抜け、出来高がそのSMAを上回り、価格がトレンドSMAを上回る。
  - **ショート**: 終値が上部バンドを下抜け、出来高がそのSMAを上回り、価格がトレンドSMAを下回り、RSI < 85。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 終値が上部バンドを上抜け。
  - **ショート**: 終値が下部バンドを下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 1.8
  - `VolumeLength` = 20
  - `TrendLength` = 80
  - `RsiLength` = 14
  - `UseLongSignals` = True
  - `UseShortSignals` = True
- **フィルター**:
  - カテゴリ: トレンド・ブレイクアウト
  - 方向: 両方
  - インジケーター: Bollinger Bands, 出来高SMA, トレンドSMA, RSI
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 4H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
