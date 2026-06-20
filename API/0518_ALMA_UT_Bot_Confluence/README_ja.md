# ALMA & UT Bot Confluence戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ALMA & UT Bot Confluence戦略は、Arnaud Legoux移動平均フィルターとUT Botスタイルのトレーリングストップを組み合わせたものです。価格が長期EMAとALMAの両方を上回り、出来高が平均を超え、RSIがモメンタムを示し、ADXがトレンドの強さを確認し、ローソク足がボリンジャーバンド上限の下にあり、UT Botが買いシグナルを生成したときにロングポジションを開きます。UT Botが弱気に転じ、同じフィルター下で価格が高速EMAを下回ったときにショートエントリーが発生します。出口はUT Botトレーリングストップ、またはATRベースの固定ストップロスおよびテイクプロフィットを使用します。

## 詳細

- **エントリー条件**:
  - ロング: 価格 > EMA & ALMA、RSI > 30、ADX > 30、価格 < ボリンジャーバンド上限、UT Bot買いシグナル、出来高およびATRフィルター、クールダウン。
  - ショート: UT Bot売りシグナルとフィルター下で価格が高速EMAを下穿き。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - UT Botトレーリングストップ、またはATRベースのストップロス/テイクプロフィットとオプションの時間出口。
- **ストップ**: ATRまたはトレーリング。
- **デフォルト値**:
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **フィルター**:
  - カテゴリ: ボラティリティフィルター付きトレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: EMA、ALMA、ADX、RSI、Bollinger Bands、UT Bot
  - ストップ: ATRまたはトレーリング
  - 複雑さ: 高
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
