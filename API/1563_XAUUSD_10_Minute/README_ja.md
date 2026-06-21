# XAUUSD 10分足戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MACD、RSI、Bollinger Bands のシグナルを使用して XAUUSD を 10 分足で取引します。強気条件が現れたときにロングポジションを建て、弱気シグナルが発生したときにショートポジションを建てます。システムは固定スプレッドを加味した ATR ベースのストップロスとテイクプロフィットを適用します。

## 詳細

- **エントリー条件**:
  - **ロング**: MACD ラインがシグナルを上抜け、RSI が売られすぎを下回る、または価格が Bollinger Bands の下限を下回る。
  - **ショート**: MACD ラインがシグナルを下抜け、RSI が買われすぎを上回る、または価格が Bollinger Bands の上限を上回る。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆シグナル、ストップロス、またはテイクプロフィットでポジションを決済。
- **ストップ**: ATR ストップロス `3 * ATR`、テイクプロフィット `5 * ATR`。
- **デフォルト値**:
  - MACD fast/slow/signal: `12/26/9`.
  - RSI period: `14`, overbought `65`, oversold `35`.
  - Bollinger length `20`, width `2`.
  - ATR period `14`.
  - Spread `38` ticks.
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 複数
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
