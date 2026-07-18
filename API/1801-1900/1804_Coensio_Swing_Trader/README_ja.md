# Coensioスイングトレーダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ユーザー定義のトレンドラインを使用したトレンドライン・ブレイクアウト戦略。戦略は上昇トレンドラインと下降トレンドラインの両方について、傾きと切片パラメーターから線形予測を計算します。終値が予測された買いラインを閾値分超えると、ロングポジションが開かれます。価格が売りラインを閾値分下回ると、ショートポジションに入ります。

ポジションはティックでの利確とストップロス値で保護されます。オプションのトレーリングストップは、価格が有利な方向に動くにつれて保護ストップを更新します。追加オプションとして、次のローソク足でブレイクアウトが失敗した場合にトレードを決済できます。

## 詳細

- **エントリー条件**:
  - ロング: `Close > BuyLine + EntryThreshold`
  - ショート: `Close < SellLine - EntryThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロス、テイクプロフィット、トレーリングストップまたは逆シグナル
- **ストップ**:
  - ティックでのテイクプロフィット
  - ティックでのストップロス
  - ティックでのオプションのトレーリングストップ
  - 次のローソク足での偽ブレイクアウト決済オプション
- **デフォルト値**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **フィルター**:
  - カテゴリ: トレンドラインブレイクアウト
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
