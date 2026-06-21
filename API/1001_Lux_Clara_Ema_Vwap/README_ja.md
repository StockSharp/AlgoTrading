# Lux Clara EMA + VWAP戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Lux Clara EMA + VWAP戦略は、VWAPと時間ウィンドウでフィルタリングしながら、速いEMAと遅いEMAのクロスオーバーを取引します。セッション中に遅いEMAがVWAPより上にある状態で速いEMAが遅いEMAを上抜けたときにロングポジションを建てます。反対の条件ではショートポジションを建てます。EMAが反対方向にクロスしたときにポジションを決済します。

## 詳細

- **エントリー条件**:
  - 速いEMAが遅いEMAを上抜け、遅いEMAがVWAPより上にあり、現在時刻がセッション内にある。
  - ショート: 速いEMAが遅いEMAを下抜け、遅いEMAがVWAPより下にあり、現在時刻がセッション内にある。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - EMAの逆方向クロス。
- **ストップ**: なし。
- **デフォルト値**:
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5分
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングとショート
  - インジケーター: EMA, VWAP
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
