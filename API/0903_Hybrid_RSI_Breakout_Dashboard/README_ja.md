# ハイブリッドRSIブレイクアウトダッシュボード
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ADXと200 EMAでフィルタリングされたブレイクアウトエントリーとRSI平均回帰を組み合わせています。

市場がレンジ相場でRSIが`RsiBuy`を下回り、EMAが強気トレンドを示している場合に買い。RSIが`RsiSell`を上回り、弱気トレンドの場合にショート売り。トレンドレジームでは、最近のクローズの上下へのブレイクアウトでエントリーし、ATRを使用してポジションをトレーリングします。

開始日フィルターと最後のトレードタイプおよび方向を示すシンプルなダッシュボード変数を含みます。

## 詳細

- **エントリー条件**: EMAバイアスを伴うレンジレジームでのRSIシグナル、またはADX > `AdxThreshold`のときに直近`BreakoutLength`本のクローズの上下へのブレイクアウト。
- **ロング/ショート**: 両方。
- **エグジット条件**: RSIトレードは`RsiExit`で終了。ブレイクアウトトレードはATRトレーリングストップを使用。
- **ストップ**: ブレイクアウトトレード用ATRトレーリングストップ。
- **デフォルト値**:
  - `AdxLength` = 14
  - `AdxThreshold` = 20m
  - `EmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuy` = 40m
  - `RsiSell` = 60m
  - `RsiExit` = 50m
  - `BreakoutLength` = 20
  - `AtrLength` = 14
  - `AtrMultiplier` = 2m
  - `StartDate` = 2017-01-01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド、平均回帰
  - 方向: 両方
  - インジケーター: ADX, EMA, RSI, ATR, Highest/Lowest
  - ストップ: トレーリング
  - 複雑さ: 中程度
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
