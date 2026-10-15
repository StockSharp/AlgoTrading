# Fxscalper戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MQL4 エキスパート「fxscalper」から翻訳されたボリンジャーバンドブレイクアウトスキャルピング戦略。
戦略はローソク足データとボリンジャーバンドを購読します。終値が上部バンドを上回ったときロングポジションを開き、終値が下部バンドを下回ったときショートポジションを開きます。ポジションはストップロスとテイクプロフィットレベルによって保護されます。

## 詳細

- **エントリー条件**:
  - ロング: `Close > Upper Band`
  - ショート: `Close < Lower Band`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナルまたは保護ストップ
- **ストップ**: ストップロスとテイクプロフィット
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **フィルター**:
  - カテゴリ: Bollinger Bands
  - 方向: 両方
  - インジケーター: Bollinger Bands
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
