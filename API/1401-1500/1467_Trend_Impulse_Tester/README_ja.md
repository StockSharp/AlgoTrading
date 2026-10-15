# Trend Impulse Tester 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Trend Impulse Tester は、EMA と ADX によって強いトレンドが確認され、RSI インパルスが現れたときにトレードに入ります。
上昇トレンド中の強気インパルスで買い、下降トレンド中の弱気インパルスで売ります。

## 詳細

- **エントリー条件**: EMA トレンド + ADX 確認と RSI がしきい値を超えること
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, ADX, RSI
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
