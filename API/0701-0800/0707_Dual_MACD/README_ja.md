# デュアル MACD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

2 つの MACD インジケーターを組み合わせた戦略です。遅い MACD のヒストグラムがゼロラインを越えたとき、速い MACD のヒストグラムが同方向を示している場合に取引を開始します。速い MACD が反転するか、ストップ/テイクプロフィットが発動したときにポジションをクローズします。

テストでは年間平均リターン約 65% が示されています。株式市場で最もよく機能します。

## 詳細

- **エントリー条件**: 遅い MACD ヒストグラムのゼロクロスと速い MACD による確認。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 速い MACD の反転またはストップ/ターゲット。
- **ストップ**: あり。
- **デフォルト値**:
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

