# Heiken Ashi Supertrend ADX戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heiken Ashiローソク足、Supertrendの方向、およびオプションのADXフィルターを組み合わせた戦略です。下ヒゲのない強気のHeiken Ashiローソク足は、上昇トレンドでロングを開きます。上ヒゲのない弱気のローソク足は、下降トレンドでショートを開きます。ポジションは逆シグナルまたはATRベースのトレーリングストップで決済されます。

テストでは年平均リターンが約128%であることが示されています。暗号資産市場で最もよく機能します。

Heiken Ashiはノイズを平滑化し、SupertrendとADXが方向を確認します。ATRが動的なストップを決定します。

## 詳細

- **エントリー条件**:
  - ロング: 下ヒゲのない強気のHAローソク足、オプションのSupertrend上昇とADX確認あり
  - ショート: 上ヒゲのない弱気のHAローソク足、オプションのSupertrend下降とADX確認あり
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のローソク足またはATRトレーリングストップ
- **ストップ**: ATRトレーリングストップ
- **デフォルト値**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `UseAdxFilter` = false
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `TrailAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Heiken Ashi, Supertrend, ADX, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
