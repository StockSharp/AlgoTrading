# RD Trend Trigger戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RD Trend Trigger戦略は、RD-TrendTriggerオシレーターを使用して、選択したモードに応じてトレンドの反転またはレベルのブレイクアウトを捉えます。twistモードでは、トレードはオシレーターの方向転換に従います。dispositionモードでは、オシレーターが所定のレベルをクロスしたときにトレードが発生します。

## 詳細

- **エントリー条件**:
  - **twistモード**: オシレーターが上向きに転換したときにロングエントリー、下向きに転換したときにショートエントリー。
  - **dispositionモード**: オシレーターが`HighLevel`を上回ったときにロングエントリー、`LowLevel`を下回ったときにショートエントリー。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル、またはdispositionモードでオシレーターが`LowLevel`を上回った際の明示的な決済条件。
- **ストップ**: デフォルトなし。外部から保護を有効にできます。
- **デフォルト値**:
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: カスタムRD-TrendTrigger（高値/安値とTillson T3に基づく）
  - ストップ: オプション
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
