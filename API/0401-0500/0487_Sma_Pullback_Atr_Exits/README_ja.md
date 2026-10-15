# SMA プルバック + ATR エグジット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、短期移動平均が長期トレンド平均の上または下にあるときにプルバックでエントリーします。価格が速い SMA を下回り、かつ速い SMA が遅い SMA を上回っているときにロングポジションを建てます。価格が速い SMA を上回り、かつ速い SMA が遅い SMA を下回っているときにショートポジションを建てます。エグジットはエントリー価格からの Average True Range の倍数を使用します。

## 詳細

- **エントリー条件**:
  - ロング: close < 速い SMA かつ 速い SMA > 遅い SMA。
  - ショート: close > 速い SMA かつ 速い SMA < 遅い SMA。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 価格が ATR ベースのストップロスまたはテイクプロフィットに達する。
- **ストップ**: ストップロスとテイクプロフィットの ATR 倍数。
- **デフォルト値**:
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, ATR
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
