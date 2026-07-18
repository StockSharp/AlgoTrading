# Parabolic SAR と MACD 確認戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はParabolic SARインジケーターとMACDの確認を組み合わせます。MACDが支持する方向にPrice がSARをクロスしたときにポジションを建て、トレンド転換を捉えることを目的とします。

## 詳細

- **エントリー条件**: 価格がSARをクロスし、MACDラインがシグナルラインの同じ側にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 価格/SARまたはMACDの逆クロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR, MACD
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: なし
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 中
