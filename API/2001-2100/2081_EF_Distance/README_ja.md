# EF Distance リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderのエキスパートアドバイザー「Exp_EF_distance」のStockSharp適応版です。元のEF DistanceインジケーターとFlat-Trendインジケーターを、市場の転換点を検出するための単純移動平均（SMA）とAverage True Range（ATR）フィルターに置き換えています。アルゴリズムは3つの連続するSMA値を監視し、ローカルな極小値または極大値を識別します。SMAがローカル底を形成し、ボラティリティが閾値を超えたときにロングポジションを開きます。逆のパターンではショートポジションを開きます。ポジションは反対シグナルまたはストップロス・テイクプロフィットレベルに達したときに決済されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `SMA(t-1) < SMA(t-2)` かつ `SMA(t) > SMA(t-1)` かつ `ATR(t) ≥ AtrThreshold`。
  - **ショート**: `SMA(t-1) > SMA(t-2)` かつ `SMA(t) < SMA(t-1)` かつ `ATR(t) ≥ AtrThreshold`。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対方向の逆シグナル。
  - ストップロスまたはテイクプロフィット到達。
- **インジケーター**:
  - 単純移動平均（SMA）– EF Distanceの近似。
  - Average True Range（ATR）– ボラティリティフィルター。
- **デフォルト値**:
  - `SMA period` = 10。
  - `ATR period` = 20。
  - `ATR threshold` = 1。
  - `StopLoss` = 100。
  - `TakeProfit` = 200。
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: 2つ
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 設定可能
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい（転換点を使用）
  - リスクレベル: 中
