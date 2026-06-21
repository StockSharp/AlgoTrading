# 寄り付き後ロング ATRストップロス・テイクプロフィット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、市場の寄り付き時に抵抗線のブレイクアウト後、価格がボリンジャーバンドの中心線付近に留まっている場合にロングポジションを建てます。EMA、RSI、ADX、ATRフィルターを使用し、ATRベースのストップロスとテイクプロフィットで退出します。

## 詳細

- **エントリー条件**:
  - **ロング**: 市場の寄り付き時に直近の抵抗線を上方ブレイク、価格がボリンジャーバンドの中心線付近、RSIが閾値を上回る、ADXが閾値を上回る、短期トレンドが上向きで押し目なし。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - ATRベースのストップロスまたはテイクプロフィット到達。
- **ストップ**:
  - ATRベースのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: Long
  - インジケーター: Bollinger Bands, EMA, RSI, ADX, ATR
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
