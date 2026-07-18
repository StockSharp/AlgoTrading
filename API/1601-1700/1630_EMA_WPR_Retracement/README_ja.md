# EMA WPR 押し目戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAトレンドフィルターとWilliams %Rの極値を組み合わせたトレンドフォロー戦略。別の取引を許可する前にWilliams %Rの押し目を待ち、設定された数までポジションをピラミッディングできます。

## 詳細

- **エントリー条件**:
  - **ロング**: Williams %Rが-100を下回り、その後`WPR Retracement`を上回る押し目が発生。EMAによるオプションの上昇トレンド確認。
  - **ショート**: Williams %Rが0を上回り、その後`-WPR Retracement`を下回る押し目が発生。EMAによるオプションの下降トレンド確認。
- **ロング/ショート**: ピラミッディングを伴う両方向。
- **エグジット条件**:
  - Williams %Rが極端な領域を離れる。
  - 利益なしで`Max Unprofit Bars`経過後のオプションのエグジット。
  - ストップロス、テイクプロフィット、オプションのトレーリングストップは保護モジュールで管理。
- **ストップ**: オプションのトレーリングストップ付き固定ストップロスとテイクプロフィット。
- **デフォルト値**:
  - `Use EMA Trend` = true
  - `Bars In Trend` = 1
  - `EMA Trend` = 144
  - `WPR Period` = 46
  - `WPR Retracement` = 30
  - `Use WPR Exit` = true
  - `Order Volume` = 0.1
  - `Max Trades` = 2
  - `Stop Loss` = 50
  - `Take Profit` = 200
  - `Use Trailing` = false
  - `Trailing Stop` = 10
  - `Use Unprofit Exit` = false
  - `Max Unprofit Bars` = 5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, Williams %R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
