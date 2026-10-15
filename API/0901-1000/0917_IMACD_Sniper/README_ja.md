# IMACD Sniper 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

IMACD SniperはMACDクロスオーバーをEMAトレンドフィルター、出来高確認、強いローソク足パターンと組み合わせます。動的なテイクプロフィットとストップロスは直近の平均レンジに基づいています。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: MACDラインがシグナルラインを上抜け、価格がEMAの上、MACDデルタ > 最小デルタ、両ラインがゼロから遠い、出来高が平均超、強い強気ローソク足。
  - **ショート**: MACDラインがシグナルラインを下抜け、価格がEMAの下、MACDデルタ > 最小デルタ、両ラインがゼロから遠い、出来高が平均超、強い弱気ローソク足。
- **エグジット条件**: 逆方向のMACDクロスまたはテイクプロフィット / ストップロスへの到達。
- **ストップ**: 平均レンジに基づく動的テイクプロフィットとストップロス。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング & ショート
  - インジケーター: MACD, EMA, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
