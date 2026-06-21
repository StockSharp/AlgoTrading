# OBV ATR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はOn-Balance Volume（OBV）を追跡し、OBVが直近の高値または安値をブレイクした際にトレードに入る。ATRブレイクアウトに似たダイナミックチャネルを維持し、強気モードと弱気モードを切り替える。

## 詳細

- **エントリー条件**: OBVが前回高値を上抜けでロング；前回安値を下抜けでショート。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたは保護注文。
- **ストップ**: はい。
- **デフォルト値**:
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: OBV, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
