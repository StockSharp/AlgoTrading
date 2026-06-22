# Color Bears Gap戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Color Bears Gapインジケーターに基づく戦略を実装します。インジケーターは高値と平滑化された始値/終値の間の2つの平滑化されたギャップを比較します。差がゼロをクロスすると、新しい方向でポジションが開かれ、反対のポジションが閉じられます。

## 詳細
- **エントリー条件**: インジケーターがゼロを下抜け -> 買い；ゼロを上抜け -> 売り。
- **ロング/ショート**: パラメーターで設定可能。
- **エグジット条件**: 反対方向のゼロラインクロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 8時間足
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: Color Bears Gap
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 8時間足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
