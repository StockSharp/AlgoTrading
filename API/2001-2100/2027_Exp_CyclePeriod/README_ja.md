# Exp CyclePeriod戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はCyclePeriodインジケーターを使用して市場サイクルの転換を検出します。インジケーターが上昇するとロングポジションを開き、下落するとショートポジションを開き、それに応じて反対のポジションを閉じます。

## 詳細

- **エントリー条件:**
  - **ロング**: CyclePeriodが上昇していて現在の値が前の値を上回っている。
  - **ショート**: CyclePeriodが下落していて現在の値が前の値を下回っている。
- **ロング/ショート**: ロングとショート。
- **エグジット条件:**
  - CyclePeriodが上向きに転じたときにショートを閉じる。
  - CyclePeriodが下向きに転じたときにロングを閉じる。
- **ストップ**: 価格単位のテイクプロフィットとストップロスを使用。
- **デフォルト値:**
  - `CandleType` = TimeSpan.FromHours(6).TimeFrame().
  - `Alpha` = 0.07.
  - `SignalBar` = 1.
  - `TakeProfit` = 2000.
  - `StopLoss` = 1000.
  - `BuyPosOpen` = true.
  - `SellPosOpen` = true.
  - `BuyPosClose` = true.
  - `SellPosClose` = true.
- **フィルター:**
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: CyclePeriod
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 6時間
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
