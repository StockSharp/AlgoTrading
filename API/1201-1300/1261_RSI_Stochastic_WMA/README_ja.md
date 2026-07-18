# RSI ストキャスティクス WMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI、ストキャスティクスオシレーター、加重移動平均 (WMA) を組み合わせた戦略です。
RSI が売られすぎ、%K が %D を上抜け、価格が WMA を上回るときに買います。
RSI が買われすぎ、%K が %D を下抜け、価格が WMA を下回るときに売ります。

## 詳細

- **エントリー条件**:
  - ロング: `RSI < 30 && %K crosses above %D && Close > WMA`
  - ショート: `RSI > 70 && %K crosses below %D && Close < WMA`
- **ロング/ショート**: 両方
- **ストップ**: なし
- **デフォルト値**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: RSI, Stochastic, WMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
