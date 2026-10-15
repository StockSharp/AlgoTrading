# VIDYA ProTrendマルチ段階利益確定戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

速いVIDYAと遅いVIDYAおよびボリンジャーバンドフィルターを使用したトレンドフォロー戦略です。
オプションとして、ATRの倍数と割合目標を使用した多段階テイクプロフィット注文を発注します。

## 詳細

- **エントリー条件**: 速いVIDYAが遅いVIDYAを上回り、価格がボリンジャーフィルターの外にある
- **ロング/ショート**: 両方
- **エグジット条件**: 反対方向の傾きまたはクロス
- **ストップ**: なし
- **デフォルト値**:
  - `FastVidyaLength` = 10
  - `SlowVidyaLength` = 30
  - `MinSlopeThreshold` = 0.05
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: VIDYA, Bollinger Bands, ATR
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
