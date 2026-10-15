# MA2CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

CCIで確認された移動平均クロス戦略。ストップロスにATRを使用します。

## 詳細

- **エントリー条件**:
  - 速いSMAが遅いSMAを上抜けし、CCIが0を上抜けたときにロング。
  - 速いSMAが遅いSMAを下抜けし、CCIが0を下抜けたときにショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆クロスまたはエントリーから1 ATRのストップロス。
- **ストップ**: エントリー価格 ± ATRに基づくATRストップ。
- **デフォルト値**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1分
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, CCI, ATR
  - ストップ: ATR
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
