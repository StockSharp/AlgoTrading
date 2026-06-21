# Combo 2/20 EMA バンドパスフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高速・低速 EMA のクロスオーバーとバンドパスフィルターを組み合わせています。高速 EMA が低速 EMA を上回り、バンドパス値が売りゾーンを突破したときにロング。高速 EMA が低速 EMA を下回り、バンドパス値が買いゾーンを下回ったときにショート。シグナルが消えた場合または開始日前にポジションをクローズします。

テストでは年間平均リターン約 47% を示しています。暗号通貨市場で最もパフォーマンスが高いです。

## 詳細
- **エントリー条件**:
  - **ロング**: 高速 EMA > 低速 EMA かつバンドパス > 売りゾーン
  - **ショート**: 高速 EMA < 低速 EMA かつバンドパス < 買いゾーン
- **ロング/ショート**: 両方
- **エグジット条件**: シグナルが消えたらポジションをクローズ
- **ストップ**: いいえ
- **デフォルト値**:
  - `FastEmaLength` = 2
  - `SlowEmaLength` = 20
  - `BpfLength` = 20
  - `BpfDelta` = 0.5m
  - `BpfSellZone` = 5m
  - `BpfBuyZone` = -5m
  - `StartDate` = new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA Bandpass Filter
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
