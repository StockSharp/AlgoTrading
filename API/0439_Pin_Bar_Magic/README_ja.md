# Pin Bar Magic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3本の移動平均で定義されたトレンド内で強気と弱気のPin Barを検出します。注文はローソク足の極値に置かれ、数バー以内に約定しない場合はキャンセルされます。ポジションサイズは資金に対するリスクパーセンテージとATRベースのストップ距離から計算されます。

この手法は重要なサポートやレジスタンスでの急激なリバーサルを捉えることを目的としています。速いEMAと中間EMAが反対方向にクロスした時にポジションを終了し、トレンドの弱まりを示します。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いEMA > 中間EMA > 遅いSMA、移動平均の一つを突き抜ける強気のPin Bar。
  - **ショート**: 速いEMA < 中間EMA < 遅いSMA、移動平均の一つを突き抜ける弱気のPin Bar。
- **エグジット条件**:
  - 速いEMAが中間EMAを反対方向にクロスする。
- **インジケーター**:
  - 遅いSMA (期間50)
  - 中間EMA (18) と速いEMA (6)
  - ATR (長さ14)
- **ストップ**: ポジションリスク = 口座のEquityRisk%、ストップはATR * 乗数の位置。
- **デフォルト値**:
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **フィルター**:
  - 価格アクションリバーサル
  - デフォルトで1時間足のローソク足で機能
  - インジケーター: EMA、SMA、ATR
  - ストップ: はい
  - 複雑さ: 高
