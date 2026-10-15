# ニュース取引EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

経済ニュース発表前後の取引を目的とした時間ベースのストラドル戦略。スケジュールされた時刻に、現在価格から固定距離の位置に対称的な買いストップと売りストップの注文を設定します。アクティベーションウィンドウ中は毎ローソク足ごとに注文が更新され、市場価格に追従します。ポジションが開かれると反対の未決注文がキャンセルされ、オプションのテイクプロフィットとストップロスの水準で決済を管理します。

## 詳細

- **エントリー条件**:
  - ストラドルウィンドウ中、close + Distance * step に買いストップ、close - Distance * step に売りストップを設定する。
- **ロング/ショート**: 両方
- **エグジット条件**: 反対ストップ、テイクプロフィット/ストップロス、または注文の期限切れ
- **ストップ**: 固定ストップロスとテイクプロフィット
- **デフォルト値**:
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: News
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イベント
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
