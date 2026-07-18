# Delta-RSIオシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はDelta-RSIオシレーターを使用します。これはEMAで平滑化されたRSIの変化量として定義されます。デルタがゼロを越えたとき、シグナルラインを越えたとき、または方向が変わったときにシグナルが発生します。エグジットは選択した条件を反映します。

## 詳細

- **エントリー条件**: Delta-RSIの`BuyCondition`（ゼロ交差、シグナルライン交差、または方向転換）に基づく。
- **ロング/ショート**: 両方、`UseLong`および`UseShort`で制御。
- **エグジット条件**: Delta-RSIの`ExitCondition`に基づく。
- **ストップ**: なし。
- **デフォルト値**:
  - `RsiLength` = 21
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: Momentum
  - 方向: 両方
  - インジケーター: RSI, EMA
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
