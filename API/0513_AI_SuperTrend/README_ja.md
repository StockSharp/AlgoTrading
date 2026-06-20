# AI SuperTrend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

AI SuperTrend 戦略は SuperTrend インジケーターと価格および SuperTrend ラインの加重移動平均を組み合わせます。SuperTrend が上向きに転換し、価格 WMA が SuperTrend WMA を上回ったときにロング取引を開始します。逆の条件でショート取引を開始します。ポジションは ATR ベースのダイナミックトレーリングストップで保護されます。

## 詳細

- **エントリー条件**:
  - **ロング**: SuperTrend の方向が上向きに転換し、価格 WMA が SuperTrend WMA を上回る。
  - **ショート**: SuperTrend の方向が下向きに転換し、価格 WMA が SuperTrend WMA を下回る。
- **エグジット条件**:
  - トレンド転換または ATR トレーリングストップ。
- **ストップ**: ダイナミック ATR トレーリングストップ。
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend, WMA, ATR
  - ストップ: あり
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
