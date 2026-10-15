# リバーサル・トラップ・スナイパー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Reversal Trap Sniperは、モメンタムがリセットされながらも価格が動き続けるRSIトラップを探す。
買われ過ぎからの反転後もより高く引けた後に買い、売られ過ぎからの反転後もより低く引けた後に売る。

## 詳細

- **エントリー条件**: 3本前のRSIが買われ過ぎ/売られ過ぎで、現在のRSIが閾値を戻り越えながら価格が同方向に継続
- **ロング/ショート**: 両方
- **エグジット条件**: ATRストップ、ターゲット、または最大バー数
- **ストップ**: ATRベース
- **デフォルト値**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: RSI, ATR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
