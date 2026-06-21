# Heiken Ashi Supertrend ATR-SL戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin AshiローソクとSupertrendの方向フィルターを組み合わせた戦略です。エントリーにはヒゲのないローソク足が必要で、ATRベースのストップロスとブレークイーブンを有効にできます。

## 詳細

- **エントリー条件**:
  - ロング: 下ヒゲのない緑のHAローソク足、上昇トレンドフィルターはオプション
  - ショート: 上ヒゲのない赤のHAローソク足、下降トレンドフィルターはオプション
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 上ヒゲのない赤のHAローソク足またはストップヒット
  - ショート: 下ヒゲのない緑のHAローソク足またはストップヒット
- **ストップ**: ATRベース、オプションのブレークイーブンあり
- **デフォルト値**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Heikin Ashi, Supertrend, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
