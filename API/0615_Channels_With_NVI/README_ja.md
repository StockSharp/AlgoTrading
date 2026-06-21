# NVIを用いたチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はボリンジャーバンドまたはケルトナーチャネルをネガティブボリュームインデックス（NVI）と組み合わせています。価格が下限バンドを下回って終値を形成し、NVIがそのEMAを上回っている場合にロングポジションを建てます。NVIがそのEMAを下回るとポジションを決済します。オプションのストップロスおよびテイクプロフィットのパーセンテージも設定できます。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値 < 下限バンド かつ NVI > NVI EMA。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - **ロング**: NVI < NVI EMA。
- **ストップ**: オプション、エントリー価格のパーセンテージ。
- **デフォルト値**:
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **フィルター**:
  - カテゴリ: チャネル
  - 方向: ロングのみ
  - インジケーター: Bollinger Bands または Keltner Channels, EMA, NVI
  - ストップ: オプション
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
