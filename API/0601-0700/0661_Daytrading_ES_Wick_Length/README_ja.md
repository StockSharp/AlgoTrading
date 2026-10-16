# ES ヒゲ長さデイトレード戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ローソク足のヒゲの合計長さが移動平均にオフセットを加えた値を超えたときにロングポジションを建て、一定のバー数保有した後にポジションを決済します。

### 検証可能な売買条件（C# / Python）

**確定1分足**のヒゲ合計を `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)` と定義します。現在の足を除く**直前20本の確定足**で `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)` を計算し、出来高がすべてゼロの場合は単純平均に切り替えます。`MaType=Simple` は常に単純平均です。`MaOffset=10` は**最小価格刻み10ティック分**で、閾値は `VWMA(W) + 10 × Security.PriceStep` です。20本のウォームアップ後、ノーポジションかつ `W > 閾値` のとき**ロングのみ**を建てます。その後**確定足18本**で残りのロングを全決済します。ショート、損切り、利確はありません。既定値: `MaLength=20`、`MaType=VolumeWeighted`、`MaOffset=10`、`HoldPeriods=18`、`CandleType=1分足`。



## 詳細

- **エントリー条件**: ヒゲの合計長さがオフセット付き移動平均より大きい。
- **エグジット条件**: `Hold periods` バー保有後にポジションを決済。
- **ロング/ショート**: ロングのみ。
- **ストップ**: なし。
- **デフォルト値**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1分足ローソク
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロング
  - インジケーター: Moving Average、ヒゲ長さ
  - ストップ: いいえ
  - 複雑さ: シンプル
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
