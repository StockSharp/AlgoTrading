# ライブRSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数のRSI計算（close、weighted、typical、median、open）とParabolic SARを使用してトレンドの反転を検出します。RSI値が強気の順序に並び価格がSARを上回るときにロングに入り、並びが弱気で価格がSARを下回るときにショートに入ります。SAR値はトレーリングストップとして機能します。

## 詳細

- **エントリー条件**:
  - RSIシーケンスが強気でかつ価格がSARを上回るときにロング。
  - RSIシーケンスが弱気でかつ価格がSARを下回るときにショート。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 反対のトレンドシグナルまたはSARトレーリングストップ。
- **ストップ**: オプションの固定ストップロスとSARベースのトレーリングストップ。
- **デフォルト値**:
  - `RSI Period` = 30
  - `SAR Step` = 0.08
  - `Stop Loss` = 40
  - `Check Hour` = false
  - `Start Hour` = 17
  - `End Hour` = 1
  - `Candle Type` = 1時間
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: RSI, Parabolic SAR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: オプション（時間フィルター）
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
