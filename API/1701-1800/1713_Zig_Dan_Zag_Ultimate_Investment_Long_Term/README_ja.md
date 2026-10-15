# Zig Dan Zag 究極の長期投資戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ZigZagのピボットと低速SMAトレンドフィルターを組み合わせた長期投資戦略です。SMAを上回る新たなZigZag安値が形成されるとポジションを開き、SMAを下回る逆方向のピボットで決済します。

## 詳細
- **エントリー条件**: SMAを上回る新たなZigZag安値。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: SMAを下回るZigZag高値。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZigzagDepth` = 12
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: Highest, Lowest, SimpleMovingAverage
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 長期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
