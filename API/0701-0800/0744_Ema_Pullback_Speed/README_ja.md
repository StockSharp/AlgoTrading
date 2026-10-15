# EMAプルバック速度戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAプルバック速度戦略は、価格の加速度に適応するダイナミックEMAを使用します。上昇トレンド中に強気の反転と十分な上昇速度を伴いながら価格がダイナミックEMAに戻ったときにロングポジションを建てます。逆の条件でショートポジションを建てます。エグジットにはATRベースのストップロスと固定パーセンテージのテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格がダイナミックEMAより上、強気の反転、価格がEMAに戻る、正の速度、短期EMAが長期EMAより上、速度 ≥ `LongSpeedMin`。
  - **ショート**: 価格がダイナミックEMAより下、弱気の反転、価格がEMAに戻る、負の速度、短期EMAが長期EMAより下、速度 ≤ `ShortSpeedMax`。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRストップロスと固定パーセンテージのテイクプロフィット。
- **ストップ**: ストップロス `AtrMultiplier`×ATR、テイクプロフィット `FixedTpPct`%。
- **デフォルト値**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, ATR
  - ストップ: ATRストップロス、固定テイクプロフィット
  - 複雑さ: 中
  - 時間軸: 5m
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
