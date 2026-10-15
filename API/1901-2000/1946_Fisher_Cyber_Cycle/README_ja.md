# Fisher Cyber Cycle戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はEhlersのCyber CycleインジケーターにFisher変換を適用します。Fisher線がトリガー線を上抜けするとロングポジションをオープンし、下抜けするとショートポジションをオープンします。逆のクロスでポジションをクローズまたは反転させます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Fisher > Trigger` && `前回 Fisher <= 前回 Trigger`
  - **ショート**: `Fisher < Trigger` && `前回 Fisher >= 前回 Trigger`
- **エグジット条件**:
  - FisherとTriggerの逆クロス
- **ストップ**: なし
- **デフォルト値**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = 8時間の時間軸
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングとショート
  - インジケーター: Fisher Transform, Cyber Cycle
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
