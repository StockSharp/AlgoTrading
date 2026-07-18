# I4 DRF v2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

I4 DRF v2戦略は、スライディングウィンドウ内の上昇と下降の終値の数を数えるカスタムi4_DRF_v2インジケーターを適用します。
TrendModesパラメーターに応じて、逆張りモード（Direct）またはトレンドフォローモード（NotDirect）で機能します。
インジケーターが符号を変えたときにポジションを開閉し、価格ステップでのオプションのストップロスとテイクプロフィットをサポートします。

## 詳細

- **エントリー条件**: TrendModesに従ったインジケーターの符号転換
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナルまたはストップロス/テイクプロフィット
- **ストップ**: はい
- **デフォルト値**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendModes` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: カスタム
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
