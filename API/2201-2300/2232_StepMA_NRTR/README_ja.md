# StepMA NRTR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

StepMA NRTRインジケーターに基づくトレンドフォロー戦略。このインジケーターは階段状の移動平均とNick Rar Trendの反転メカニズムを組み合わせ、トレンドが変化したときに売買シグナルを生成します。

## 詳細

- **エントリー条件**: StepMA NRTRの買い/売りシグナル
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のStepMA NRTRシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `Length` = 10
  - `Kv` = 1
  - `StepSize` = 0
  - `UseHighLow` = true
  - `CandleType` = 時間軸1時間
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: StepMA NRTR
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
