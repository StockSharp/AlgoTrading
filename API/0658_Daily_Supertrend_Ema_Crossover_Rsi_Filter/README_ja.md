# 日足 Supertrend EMA クロスオーバー RSI フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SupertrendがトレンドとRSIが有利な条件を確認した場合にのみEMAクロスオーバーを取引する戦略。ATRベースのストップロスとテイクプロフィットレベルを使用します。

## 詳細

- **エントリー条件**:
  - ロング: `Fast EMA`が`Slow EMA`を上抜け、Supertrendが上昇トレンド、`RSI < RsiOverbought`
  - ショート: `Fast EMA`が`Slow EMA`を下抜け、Supertrendが下降トレンド、`RSI > RsiOversold`
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップロスまたはテイクプロフィット
- **ストップ**: はい
- **デフォルト値**:
  - `FastEmaLength` = 3
  - `SlowEmaLength` = 6
  - `AtrLength` = 3
  - `StopLossMultiplier` = 2.5m
  - `TakeProfitMultiplier` = 4m
  - `RsiLength` = 10
  - `RsiOverbought` = 65m
  - `RsiOversold` = 30m
  - `SupertrendMultiplier` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, Supertrend, RSI, ATR
  - ストップ: ATR倍数
  - 複雑さ: 中級
  - 時間軸: 長期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
