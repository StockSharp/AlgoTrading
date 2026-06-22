# FRASMAv2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Fractal Adaptive Simple Moving Average（FRASMAv2）に基づく戦略。

この戦略はFractal Dimensionインジケーターを使用してFractal Adaptive Simple Moving Averageを計算します。インジケーターの色は傾きに応じて変化します：上昇時は緑、横ばい時はグレー、下降時はマゼンタ。戦略は最後の確定足における色の変化を監視します：

- 前のバーでインジケーターが緑で、最後のバーで緑以外（グレーまたはマゼンタ）になった場合、戦略はショートポジションを決済し、新しいロングポジションを開きます。
- インジケーターがマゼンタで、マゼンタ以外になった場合、戦略はロングポジションを決済し、新しいショートポジションを開きます。

リスク管理はポイントで指定したストップロスとテイクプロフィットのパラメーターを使用します。

## 詳細

- **エントリー条件**：FRASMAv2の色変化。
- **ロング/ショート**：両方向。
- **エグジット条件**：反対の色変化。
- **ストップ**：保護モジュールによるテイクプロフィットとストップロス。
- **デフォルト値**：
  - `Period` = 30
  - `TakeProfit` = 2000ポイント
  - `StopLoss` = 1000ポイント
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**：
  - カテゴリ：トレンド転換
  - 方向：両方
  - インジケーター：FractalDimension, FRASMAv2
  - ストップ：はい
  - 複雑さ：中級
  - 時間軸：4h
  - 季節性：いいえ
  - ニューラルネットワーク：いいえ
  - ダイバージェンス：いいえ
  - リスクレベル：中
