# FlexiSuperTrend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はSupertrendフィルターと平滑化された偏差オシレーターを組み合わせます。
価格がSupertrendの方向と一致し、オシレーターがモメンタムを確認したときにポジションを開きます。

## 詳細

- **エントリー条件**:
  - 価格がSupertrendを上回り、偏差（価格からSupertrendを引いたSMA）> 0 → 買い。
  - 価格がSupertrendを下回り、偏差 < 0 → 売り。
- **ロング/ショート**: 両方向を有効にできます。
- **エグジット条件**:
  - 価格がSupertrend線を突破したときのトレンド反転。
- **ストップ**: デフォルトではストップロジックなし。
- **デフォルト値**:
  - ATR期間 = 10。
  - ATRファクター = 3.0。
  - SMA長さ = 10。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend, SMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
