# Aroon WPR Crossover戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Aroonクロスオーバーとウィリアムズ%Rモメンタムフィルターを組み合わせたトレンドフォロー戦略。高速Aroon Upラインが売られすぎ環境をWilliams %Rが確認する中でAroon Downを上抜けすると、ロング取引が開始されます。ショート取引はWilliams %Rが買われすぎ領域にある場合の逆ロジックに従います。オープンポジションはWilliams %Rの反転、または価格ステップで測定されたオプションのストップロスとテイクプロフィットレベルで決済できます。

## 詳細

- **エントリー条件**:
  - ロング: Aroon UpがAroon Downを上抜けし、Williams %R < `-(100 - OpenWprLevel)`
  - ショート: Aroon DownがAroon Upを上抜けし、Williams %R > `-OpenWprLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - Williams %Rが`CloseWprLevel`で定義された売られすぎ/買われすぎゾーンを抜け出す
  - 価格ステップでのオプションのテイクプロフィットとストップロスしきい値
- **ストップ**: 価格ステップでのオプションの固定ストップロスとテイクプロフィット
- **デフォルト値**:
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m (無効)
  - `StopLossSteps` = 0m (無効)
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Aroon, Williams %R
  - ストップ: オプション
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中程度
